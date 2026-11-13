/*
 * selection_model.c — portable reference core for DiskWise (design prototype)
 *
 * STATUS: This is a host-portable logic prototype written for this report.
 * It is NOT part of the user's existing application and has not been compiled
 * against the Windows SDK. It exists to verify two algorithms that are easy to
 * get wrong and dangerous when wrong:
 *
 *   1. Selection semantics for Requirement 2 — selections persisted by stable
 *      file identity across searches/filters, with explicit accounting of
 *      selected-but-hidden items.
 *   2. The pre-deletion safety gate for Requirement 3 / Part 3.1 — identity
 *      re-verification, protected-path blocking, hardlink/reparse/lock policy.
 *
 * The Windows layer would supply FILE_ID_INFO (volume serial + 128-bit file id),
 * allocation size, attribute flags, and lock state; everything below is pure C11.
 */
#include "selection_model.h"

#include <stdlib.h>
#include <string.h>

#define LOAD_NUM 7
#define LOAD_DEN 10
#define EMPTY 0u
#define USED 1u
#define TOMB 2u

static uint64_t mix64(uint64_t x)
{
    x ^= x >> 33; x *= 0xff51afd7ed558ccdull;
    x ^= x >> 33; x *= 0xc4ceb9fe1a85ec53ull;
    x ^= x >> 33;
    return x;
}

static size_t key_hash(dw_key k)
{
    return (size_t)(mix64(k.vol ^ mix64(k.hi ^ mix64(k.lo))));
}

static int key_eq(dw_key a, dw_key b)
{
    return a.vol == b.vol && a.hi == b.hi && a.lo == b.lo;
}

dw_sel *dw_sel_create(size_t initial_capacity)
{
    size_t cap = 16;
    while (cap < initial_capacity) cap <<= 1;

    dw_sel *s = calloc(1, sizeof *s);
    if (!s) return NULL;
    s->slots = calloc(cap, sizeof *s->slots);
    if (!s->slots) { free(s); return NULL; }
    s->cap = cap;
    return s;
}

void dw_sel_destroy(dw_sel *s)
{
    if (!s) return;
    free(s->slots);
    free(s);
}

static int sel_grow(dw_sel *s, size_t need)
{
    if ((s->used + 1) * LOAD_DEN <= s->cap * LOAD_NUM && s->used + 1 < s->cap) return 0;
    (void)need;

    size_t ncap = s->cap << 1;
    dw_slot *ns = calloc(ncap, sizeof *ns);
    if (!ns) return -1;

    for (size_t i = 0; i < s->cap; ++i) {
        if (s->slots[i].state != USED) continue;
        size_t j = key_hash(s->slots[i].key) & (ncap - 1);
        while (ns[j].state == USED) j = (j + 1) & (ncap - 1);
        ns[j] = s->slots[i];
    }
    free(s->slots);
    s->slots = ns;
    s->cap = ncap;
    s->tomb = 0;
    return 0;
}

/* returns 1 if newly added, 0 if already present, -1 on allocation failure */
int dw_sel_add(dw_sel *s, dw_key key, uint64_t bytes)
{
    if (sel_grow(s, 0) != 0) return -1;

    size_t i = key_hash(key) & (s->cap - 1);
    size_t first_tomb = (size_t)-1;
    for (;;) {
        dw_slot *sl = &s->slots[i];
        if (sl->state == EMPTY) {
            if (first_tomb != (size_t)-1) sl = &s->slots[first_tomb], s->tomb--;
            sl->state = USED; sl->key = key; sl->bytes = bytes;
            s->used++;
            return 1;
        }
        if (sl->state == TOMB) {
            if (first_tomb == (size_t)-1) first_tomb = i;
        } else if (key_eq(sl->key, key)) {
            sl->bytes = bytes;          /* refresh size; preserves selection */
            return 0;
        }
        i = (i + 1) & (s->cap - 1);
    }
}

int dw_sel_remove(dw_sel *s, dw_key key)
{
    if (!s || s->used == 0) return 0;
    size_t i = key_hash(key) & (s->cap - 1);
    for (;;) {
        dw_slot *sl = &s->slots[i];
        if (sl->state == EMPTY) return 0;
        if (sl->state == USED && key_eq(sl->key, key)) {
            sl->state = TOMB;
            s->used--; s->tomb++;
            return 1;
        }
        i = (i + 1) & (s->cap - 1);
    }
}

int dw_sel_contains(const dw_sel *s, dw_key key)
{
    if (!s || s->used == 0) return 0;
    size_t i = key_hash(key) & (s->cap - 1);
    for (;;) {
        const dw_slot *sl = &s->slots[i];
        if (sl->state == EMPTY) return 0;
        if (sl->state == USED && key_eq(sl->key, key)) return 1;
        i = (i + 1) & (s->cap - 1);
    }
}

void dw_sel_clear(dw_sel *s)
{
    if (!s) return;
    memset(s->slots, 0, s->cap * sizeof *s->slots);
    s->used = s->tomb = 0;
}

size_t dw_sel_count(const dw_sel *s) { return s ? s->used : 0; }

uint64_t dw_sel_total_bytes(const dw_sel *s)
{
    if (!s) return 0;
    uint64_t t = 0;
    for (size_t i = 0; i < s->cap; ++i)
        if (s->slots[i].state == USED) t += s->slots[i].bytes;
    return t;
}

static int rec_visible(const dw_rec *r, dw_filter_fn f, void *ctx)
{
    return r->exists && (f == NULL || f(ctx, r));
}

/*
 * Recomputes the view and, in the same pass, produces the accounting that the
 * confirmation dialog needs. selected_hidden_* is the number of chosen items the
 * user cannot currently see — the exact quantity a naive "select all visible"
 * implementation silently loses.
 */
dw_sel_stats dw_sel_apply_view(dw_sel *s, dw_rec *recs, size_t n,
                               dw_filter_fn visible_filter, void *ctx)
{
    dw_sel_stats st = {0};
    for (size_t i = 0; i < n; ++i) {
        dw_rec *r = &recs[i];
        r->visible = rec_visible(r, visible_filter, ctx);
        int sel = dw_sel_contains(s, r->key);
        if (r->visible) st.visible_count++;
        if (sel) {
            if (r->visible) {
                st.selected_visible++;
                st.selected_visible_bytes += r->bytes;
            } else {
                st.selected_hidden++;
                /* hidden selected items may be filtered out, deleted from disk,
                   or both; only count bytes we can still attribute */
                if (r->exists) st.selected_hidden_bytes += r->bytes;
            }
        }
    }
    st.selected_total = dw_sel_count(s);
    st.selected_total_bytes = dw_sel_total_bytes(s);
    return st;
}

size_t dw_sel_select_visible(dw_sel *s, const dw_rec *recs, size_t n)
{
    size_t added = 0;
    for (size_t i = 0; i < n; ++i)
        if (recs[i].visible && recs[i].exists)
            if (dw_sel_add(s, recs[i].key, recs[i].bytes) == 1) added++;
    return added;
}

size_t dw_sel_select_matching(dw_sel *s, const dw_rec *recs, size_t n,
                              dw_filter_fn match_filter, void *ctx)
{
    size_t added = 0;
    for (size_t i = 0; i < n; ++i)
        if (rec_visible(&recs[i], match_filter, ctx) &&
            dw_sel_add(s, recs[i].key, recs[i].bytes) == 1) added++;
    return added;
}

size_t dw_sel_deselect_visible(dw_sel *s, const dw_rec *recs, size_t n)
{
    size_t removed = 0;
    for (size_t i = 0; i < n; ++i)
        if (recs[i].visible && dw_sel_remove(s, recs[i].key)) removed++;
    return removed;
}

/* Inverts only what the user can see; hidden selections are untouched. */
dw_sel_stats dw_sel_invert_visible(dw_sel *s, dw_rec *recs, size_t n,
                                   dw_filter_fn visible_filter, void *ctx)
{
    for (size_t i = 0; i < n; ++i) {
        dw_rec *r = &recs[i];
        r->visible = rec_visible(r, visible_filter, ctx);
        if (!r->visible || !r->exists) continue;
        if (dw_sel_contains(s, r->key)) dw_sel_remove(s, r->key);
        else                            dw_sel_add(s, r->key, r->bytes);
    }
    return dw_sel_apply_view(s, recs, n, visible_filter, ctx);
}

/*
 * After a rescan, selections whose file identity no longer exists are marked
 * stale rather than silently dropped or blindly kept. A destructive job must be
 * blocked if any stale selection would otherwise be acted on.
 */
dw_sel_stats dw_sel_reconcile(dw_sel *s, dw_rec *recs, size_t n,
                              dw_filter_fn visible_filter, void *ctx)
{
    /* build presence by scanning all records (O(n); called once per rescan) */
    for (size_t i = 0; i < n; ++i) {
        if (dw_sel_contains(s, recs[i].key)) {
            dw_sel_add(s, recs[i].key, recs[i].bytes); /* refresh size */
        }
    }
    dw_sel_stats st = dw_sel_apply_view(s, recs, n, visible_filter, ctx);
    st.stale = 0;
    for (size_t i = 0; i < s->cap; ++i) {
        if (s->slots[i].state != USED) continue;
        int found = 0;
        for (size_t j = 0; j < n && !found; ++j)
            if (recs[j].exists && key_eq(recs[j].key, s->slots[i].key)) found = 1;
        if (!found) st.stale++;
    }
    return st;
}

/* ---------------------------------------------------------------- gate ---- */

static int has_prefix(const char *path, const char *prefix)
{
    size_t n = strlen(prefix);
    if (n == 0) return 0;
    for (size_t i = 0; i < n; ++i) {
        char a = path[i], b = prefix[i];
        if (a == '\0') return 0;
        if (a >= 'A' && a <= 'Z') a = (char)(a - 'A' + 'a');
        if (b >= 'A' && b <= 'Z') b = (char)(b - 'A' + 'a');
        if (a != b) return 0;
    }
    return 1;
}

dw_verdict dw_gate_evaluate(const dw_rec *current, dw_key expected,
                            const dw_policy *pol, dw_reason *reasons, size_t max_reasons)
{
    size_t nr = 0;
#define ADD_REASON(code) do { if (nr < max_reasons) reasons[nr++] = (code); } while (0)

    if (!current || !current->exists) { ADD_REASON(DW_R_GONE); return DW_BLOCK; }

    /* 1. Identity: identity mismatch means the path now holds a different file. */
    if (!key_eq(current->key, expected)) { ADD_REASON(DW_R_IDENTITY_CHANGED); return DW_BLOCK; }

    /* 2. Protected locations trump every other consideration. */
    for (size_t i = 0; pol && i < pol->protected_count; ++i) {
        if (has_prefix(current->path, pol->protected_prefixes[i])) {
            ADD_REASON(DW_R_PROTECTED_PATH);
            return DW_BLOCK;
        }
    }

    /* 3. Structural hazards. */
    if (pol && pol->never_follow_reparse && (current->flags & DW_F_REPARSE))
        { ADD_REASON(DW_R_REPARSE); return DW_BLOCK; }
    if (pol && pol->block_system_attributes && (current->flags & DW_F_SYSTEM))
        { ADD_REASON(DW_R_SYSTEM_FILE); return DW_BLOCK; }

    dw_verdict v = DW_ALLOW;

    if (current->flags & DW_F_LOCKED)      { ADD_REASON(DW_R_LOCKED);      v = DW_WARN; }
    if (current->flags & DW_F_HARDLINK)    { ADD_REASON(DW_R_HARDLINK);    v = DW_WARN; }
    if (current->flags & DW_F_CLOUD_PLACEHOLDER)
                                           { ADD_REASON(DW_R_CLOUD);       v = DW_WARN; }
    if (current->flags & DW_F_RECENTLY_USED)
                                           { ADD_REASON(DW_R_RECENT_USE);  v = DW_WARN; }
    if (pol && current->bytes > pol->size_warn_threshold)
                                           { ADD_REASON(DW_R_SIZE_THRESHOLD); v = DW_WARN; }
    if (pol && pol->max_risk_allowed < current->risk)
                                           { ADD_REASON(DW_R_RISK_EXCEEDED);   v = DW_BLOCK; }

    if (v == DW_BLOCK && nr == 0) ADD_REASON(DW_R_RISK_EXCEEDED);
    return v;
#undef ADD_REASON
}
