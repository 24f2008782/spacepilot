/* tests.c — behavioural tests for the DiskWise selection/safety reference core.
 * Build and run:  make test   (uses ASan + UBSan)
 */
#include "selection_model.h"

#include <stdio.h>
#include <stdlib.h>
#include <string.h>

static int g_pass = 0, g_fail = 0;

#define CHECK(cond, msg)                                                       \
    do {                                                                       \
        if (cond) { g_pass++; printf("  PASS  %s\n", msg); }                    \
        else      { g_fail++; printf("  FAIL  %s  (line %d)\n", msg, __LINE__); }\
    } while (0)

const char *dw_reason_text(dw_reason r)
{
    switch (r) {
    case DW_R_NONE:             return "no issue";
    case DW_R_GONE:             return "item no longer exists";
    case DW_R_IDENTITY_CHANGED: return "path now holds a different file";
    case DW_R_PROTECTED_PATH:   return "protected system location";
    case DW_R_REPARSE:          return "reparse point / junction not followed";
    case DW_R_SYSTEM_FILE:      return "system attribute set";
    case DW_R_LOCKED:           return "file is in use";
    case DW_R_HARDLINK:         return "file has multiple hard links";
    case DW_R_CLOUD:            return "cloud placeholder (no local data)";
    case DW_R_RECENT_USE:       return "recent usage evidence";
    case DW_R_SIZE_THRESHOLD:   return "larger than review threshold";
    case DW_R_RISK_EXCEEDED:    return "risk class not permitted by policy";
    }
    return "unknown";
}

static dw_key K(uint64_t id) { dw_key k = { 7, 0, id }; return k; }

static int f_large(void *ctx, const dw_rec *r)   { (void)ctx; return r->bytes > 1000; }
static int f_narrow(void *ctx, const dw_rec *r)  { (void)ctx; return strstr(r->path, "old") != NULL; }

static dw_rec mk(uint64_t id, uint64_t bytes, const char *path, uint32_t flags, uint8_t risk)
{
    dw_rec r; memset(&r, 0, sizeof r);
    r.key = K(id); r.bytes = bytes; r.path = path; r.flags = flags;
    r.risk = risk; r.exists = 1; r.visible = 0;
    return r;
}

static const char *const g_protected[] = {
    "C:\\Windows\\",
    "C:\\Program Files\\WindowsApps\\",
    "C:\\System Volume Information\\"
};

static void test_selection_persistence(void)
{
    puts("[selection] persistence across filters");
    dw_sel *s = dw_sel_create(8);
    dw_rec recs[5];
    recs[0] = mk(1, 5000, "C:\\Users\\a\\Downloads\\old-setup.exe", 0, 0);
    recs[1] = mk(2, 900,  "C:\\Users\\a\\Docs\\note.txt",           0, 0);
    recs[2] = mk(3, 7000, "C:\\Users\\a\\Downloads\\old-video.mp4", 0, 0);
    recs[3] = mk(4, 20000,"C:\\Games\\archive\\big.bin",            0, 0);
    recs[4] = mk(5, 100,  "C:\\Users\\a\\Docs\\old-small.txt",      0, 0);

    dw_sel_stats st = dw_sel_apply_view(s, recs, 5, f_large, NULL);
    CHECK(st.visible_count == 3, "primary search shows 3 large records");
    dw_sel_select_visible(s, recs, 5);
    CHECK(dw_sel_count(s) == 3, "select-visible selects exactly those 3");

    /* narrow the view with a second predicate level ("search within results") */
    dw_rec narrowed[5];
    memcpy(narrowed, recs, sizeof recs);
    st = dw_sel_apply_view(s, narrowed, 5, f_narrow, NULL);
    CHECK(st.visible_count == 3, "narrow view shows 3 paths containing 'old'");
    CHECK(st.selected_visible == 2, "2 of the selections remain visible");
    CHECK(st.selected_hidden == 1, "1 selected item is hidden by the narrow filter");
    CHECK(st.selected_hidden_bytes == 20000, "hidden selection contributes 20000 bytes");
    CHECK(st.selected_total == 3, "selection itself is unchanged by filtering");
    CHECK(st.selected_total_bytes == 32000, "total selected size stays 32000");

    /* widen the view again: the hidden selection reappears */
    st = dw_sel_apply_view(s, recs, 5, NULL, NULL);
    CHECK(st.visible_count == 5, "clearing filters shows all 5 rows again");
    CHECK(st.selected_visible == 3 && st.selected_hidden == 0,
          "the previously hidden selection is restored intact");
    dw_sel_destroy(s);
}

static void test_bulk_ops(void)
{
    puts("[selection] bulk operations respect the view, not the cursor");
    dw_sel *s = dw_sel_create(8);
    dw_rec recs[6];
    recs[0] = mk(10, 2000, "C:\\a\\old1.bin", 0, 0);
    recs[1] = mk(11, 2000, "C:\\a\\old2.bin", 0, 0);
    recs[2] = mk(12, 2000, "C:\\a\\new1.bin", 0, 0);
    recs[3] = mk(13, 2000, "C:\\a\\new2.bin", 0, 0);
    recs[4] = mk(14, 500,  "C:\\a\\old3.bin", 0, 0);
    recs[5] = mk(15, 500,  "C:\\a\\new3.bin", 0, 0);

    dw_sel_apply_view(s, recs, 6, f_large, NULL);
    size_t added = dw_sel_select_matching(s, recs, 6, f_large, NULL);
    CHECK(added == 4, "select-all-matching adds 4 large records");

    /* invert only the narrow view (paths containing "old"): 3 visible, of which 2 selected */
    dw_sel_stats st = dw_sel_invert_visible(s, recs, 6, f_narrow, NULL);
    CHECK(st.visible_count == 3, "narrow view has 3 rows");
    CHECK(dw_sel_count(s) == 3, "invert adds record 4 and removes 10 and 11");
    CHECK(dw_sel_contains(s, K(12)) && dw_sel_contains(s, K(13)), "records outside the view were untouched");
    CHECK(dw_sel_contains(s, K(14)), "invert selects visible unselected record 14");

    size_t removed = dw_sel_deselect_visible(s, recs, 6);
    CHECK(removed == 1, "deselect-visible removes only the one visible selection");

    dw_sel_clear(s);
    CHECK(dw_sel_count(s) == 0 && dw_sel_total_bytes(s) == 0, "clear resets count and size");
    dw_sel_destroy(s);
}

static void test_reconcile(void)
{
    puts("[selection] reconciliation after rescan");
    dw_sel *s = dw_sel_create(8);
    dw_rec recs[4];
    recs[0] = mk(20, 1000, "C:\\a\\one", 0, 0);
    recs[1] = mk(21, 1000, "C:\\a\\two", 0, 0);
    recs[2] = mk(22, 1000, "C:\\a\\three", 0, 0);
    recs[3] = mk(23, 1000, "C:\\a\\four", 0, 0);
    dw_sel_apply_view(s, recs, 4, NULL, NULL);
    dw_sel_select_visible(s, recs, 4);
    CHECK(dw_sel_count(s) == 4, "all four selected");

    recs[1].exists = 0;               /* deleted outside the app */
    recs[2].bytes  = 5000;            /* size changed by another program */
    dw_sel_stats st = dw_sel_reconcile(s, recs, 4, NULL, NULL);
    CHECK(st.stale == 1, "one stale selection detected");
    CHECK(st.selected_total == 4, "stale selection is not silently dropped");
    CHECK(st.selected_hidden == 1, "vanished item is reported as hidden/unavailable");
    CHECK(dw_sel_total_bytes(s) == 8000, "total refreshed to current sizes (5000 + 1000*3)");
    dw_sel_destroy(s);
}

static void test_gate(void)
{
    puts("[gate] pre-deletion safety verdicts");
    dw_policy pol; memset(&pol, 0, sizeof pol);
    pol.protected_prefixes = g_protected;
    pol.protected_count = 3;
    pol.size_warn_threshold = 10 * 1024 * 1024;
    pol.max_risk_allowed = 1;          /* refuse risk >= high (2) */
    pol.never_follow_reparse = 1;
    pol.block_system_attributes = 1;

    dw_reason rs[8];
    dw_rec ok = mk(30, 4096, "C:\\Users\\a\\Downloads\\x.tmp", 0, 0);
    CHECK(dw_gate_evaluate(&ok, ok.key, &pol, rs, 8) == DW_ALLOW, "ordinary temp file allowed");

    dw_rec moved = mk(31, 4096, "C:\\Users\\a\\Downloads\\x.tmp", 0, 0);
    CHECK(dw_gate_evaluate(&moved, K(99), &pol, rs, 8) == DW_BLOCK &&
          rs[0] == DW_R_IDENTITY_CHANGED,
          "identity mismatch blocks (path reused by another file)");

    dw_rec prot = mk(32, 4096, "C:\\Windows\\System32\\drivers\\x.sys", 0, 0);
    CHECK(dw_gate_evaluate(&prot, prot.key, &pol, rs, 8) == DW_BLOCK &&
          rs[0] == DW_R_PROTECTED_PATH, "protected prefix blocks");

    dw_rec junc = mk(33, 4096, "C:\\Users\\a\\link", DW_F_REPARSE, 0);
    CHECK(dw_gate_evaluate(&junc, junc.key, &pol, rs, 8) == DW_BLOCK &&
          rs[0] == DW_R_REPARSE, "junctions/symlinks are not followed");

    dw_rec sys = mk(34, 4096, "C:\\Users\\a\\pagefile.sys", DW_F_SYSTEM, 0);
    CHECK(dw_gate_evaluate(&sys, sys.key, &pol, rs, 8) == DW_BLOCK &&
          rs[0] == DW_R_SYSTEM_FILE, "system-attribute file blocked by policy");

    dw_rec locked = mk(35, 4096, "C:\\Users\\a\\AppData\\Local\\T\\cache.bin", DW_F_LOCKED, 0);
    CHECK(dw_gate_evaluate(&locked, locked.key, &pol, rs, 8) == DW_WARN &&
          rs[0] == DW_R_LOCKED, "locked file warns");

    dw_rec hard = mk(36, 4096, "C:\\Users\\a\\AppData\\Local\\T\\shared.dll", DW_F_HARDLINK, 0);
    CHECK(dw_gate_evaluate(&hard, hard.key, &pol, rs, 8) == DW_WARN &&
          rs[0] == DW_R_HARDLINK, "hard-linked file warns");

    dw_rec cloud = mk(37, 4096, "C:\\Users\\a\\OneDrive\\doc.docx", DW_F_CLOUD_PLACEHOLDER, 0);
    CHECK(dw_gate_evaluate(&cloud, cloud.key, &pol, rs, 8) == DW_WARN &&
          rs[0] == DW_R_CLOUD, "cloud placeholder warns (no local data to reclaim)");

    dw_rec recent = mk(38, 4096, "C:\\Users\\a\\Documents\\recent.pdf", DW_F_RECENTLY_USED, 0);
    CHECK(dw_gate_evaluate(&recent, recent.key, &pol, rs, 8) == DW_WARN &&
          rs[0] == DW_R_RECENT_USE, "recent-use evidence warns");

    dw_rec high = mk(39, 4096, "C:\\Users\\a\\AppData\\Roaming\\App\\state.db", 0, 2);
    CHECK(dw_gate_evaluate(&high, high.key, &pol, rs, 8) == DW_BLOCK &&
          rs[0] == DW_R_RISK_EXCEEDED, "risk above policy ceiling blocks");

    dw_rec gone = mk(40, 4096, "C:\\Users\\a\\temp2.tmp", 0, 0);
    gone.exists = 0;
    CHECK(dw_gate_evaluate(&gone, gone.key, &pol, rs, 8) == DW_BLOCK &&
          rs[0] == DW_R_GONE, "missing item blocks");

    CHECK(dw_gate_evaluate(NULL, ok.key, &pol, rs, 8) == DW_BLOCK, "null record blocks");
}

static void test_scale(void)
{
    puts("[scale] 200k selections: membership + accounting stay consistent");
    const size_t N = 200000;
    dw_sel *s = dw_sel_create(1024);
    dw_rec *recs = calloc(N, sizeof *recs);
    for (size_t i = 0; i < N; ++i) {
        recs[i] = mk(1000 + i, 100, "C:\\bulk\\file.bin", 0, 0);
        dw_sel_add(s, recs[i].key, recs[i].bytes);
    }
    CHECK(dw_sel_count(s) == N, "200000 distinct keys inserted without loss");
    CHECK(dw_sel_total_bytes(s) == (uint64_t)N * 100, "aggregate size is exact");

    for (size_t i = 0; i < N; i += 2) dw_sel_remove(s, recs[i].key);
    CHECK(dw_sel_count(s) == N / 2, "half removed via tombstones");
    CHECK(dw_sel_contains(s, K(1001)) && !dw_sel_contains(s, K(1000)), "membership remains correct");

    dw_sel_stats st = dw_sel_apply_view(s, recs, N, NULL, NULL);
    CHECK(st.selected_visible == N / 2 && st.selected_hidden == 0, "view accounting matches");
    free(recs);
    dw_sel_destroy(s);
}

int main(void)
{
    puts("DiskWise selection/safety core — behavioural tests\n");
    test_selection_persistence();
    test_bulk_ops();
    test_reconcile();
    test_gate();
    test_scale();
    printf("\n%d passed, %d failed\n", g_pass, g_fail);
    return g_fail == 0 ? 0 : 1;
}
