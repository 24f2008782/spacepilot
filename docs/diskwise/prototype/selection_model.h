/* selection_model.h — DiskWise selection/safety reference core (design prototype) */
#ifndef DISKWISE_SELECTION_MODEL_H
#define DISKWISE_SELECTION_MODEL_H

#include <stddef.h>
#include <stdint.h>

/* Stable file identity: volume serial + 128-bit file id (FILE_ID_INFO). */
typedef struct { uint64_t vol, hi, lo; } dw_key;

enum {
    DW_F_REPARSE           = 1u << 0,
    DW_F_HARDLINK          = 1u << 1,
    DW_F_LOCKED            = 1u << 2,
    DW_F_SYSTEM            = 1u << 3,
    DW_F_CLOUD_PLACEHOLDER = 1u << 4,
    DW_F_RECENTLY_USED     = 1u << 5
};

typedef struct {
    dw_key      key;
    uint64_t    bytes;      /* allocation size, not logical length */
    uint32_t    flags;      /* DW_F_* */
    uint8_t     risk;       /* 0 safe, 1 review, 2 high, 3 protected */
    uint8_t     exists;     /* 0 after a rescan if the file is gone */
    uint8_t     visible;    /* output: passes the current view filter */
    const char *path;
} dw_rec;

typedef struct { dw_key key; uint64_t bytes; uint8_t state; } dw_slot;

typedef struct {
    dw_slot *slots;
    size_t   cap, used, tomb;
} dw_sel;

typedef struct {
    size_t   visible_count;
    size_t   selected_visible, selected_hidden, selected_total, stale;
    uint64_t selected_visible_bytes, selected_hidden_bytes, selected_total_bytes;
} dw_sel_stats;

typedef int (*dw_filter_fn)(void *ctx, const dw_rec *r);

dw_sel  *dw_sel_create(size_t initial_capacity);
void     dw_sel_destroy(dw_sel *s);
int      dw_sel_add(dw_sel *s, dw_key key, uint64_t bytes);
int      dw_sel_remove(dw_sel *s, dw_key key);
int      dw_sel_contains(const dw_sel *s, dw_key key);
void     dw_sel_clear(dw_sel *s);
size_t   dw_sel_count(const dw_sel *s);
uint64_t dw_sel_total_bytes(const dw_sel *s);

dw_sel_stats dw_sel_apply_view(dw_sel *s, dw_rec *recs, size_t n,
                               dw_filter_fn visible_filter, void *ctx);
dw_sel_stats dw_sel_invert_visible(dw_sel *s, dw_rec *recs, size_t n,
                                   dw_filter_fn visible_filter, void *ctx);
dw_sel_stats dw_sel_reconcile(dw_sel *s, dw_rec *recs, size_t n,
                              dw_filter_fn visible_filter, void *ctx);
size_t dw_sel_select_visible(dw_sel *s, const dw_rec *recs, size_t n);
size_t dw_sel_select_matching(dw_sel *s, const dw_rec *recs, size_t n,
                              dw_filter_fn match_filter, void *ctx);
size_t dw_sel_deselect_visible(dw_sel *s, const dw_rec *recs, size_t n);

/* ---- pre-deletion gate ---- */
typedef enum { DW_ALLOW = 0, DW_WARN = 1, DW_BLOCK = 2 } dw_verdict;

typedef enum {
    DW_R_NONE = 0, DW_R_GONE, DW_R_IDENTITY_CHANGED, DW_R_PROTECTED_PATH,
    DW_R_REPARSE, DW_R_SYSTEM_FILE, DW_R_LOCKED, DW_R_HARDLINK, DW_R_CLOUD,
    DW_R_RECENT_USE, DW_R_SIZE_THRESHOLD, DW_R_RISK_EXCEEDED
} dw_reason;

typedef struct {
    const char *const *protected_prefixes;
    size_t             protected_count;
    uint64_t           size_warn_threshold;
    uint8_t            max_risk_allowed;
    int                never_follow_reparse;
    int                block_system_attributes;
} dw_policy;

dw_verdict dw_gate_evaluate(const dw_rec *current, dw_key expected,
                            const dw_policy *pol, dw_reason *reasons, size_t max_reasons);

const char *dw_reason_text(dw_reason r);

#endif
