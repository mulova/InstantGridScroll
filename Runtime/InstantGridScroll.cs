using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace mulova.ugui
{
    [ExecuteInEditMode]
    public class InstantGridScroll : ScrollRect
    {
        internal class ItemData
        {
            internal readonly int index;
            internal Vector2 pos;
            internal Bounds bounds;

            internal ItemData(int i)
            {
                this.index = i;
            }

            public override string ToString()
            {
                return $"({pos.x},{pos.y})";
            }

            public override bool Equals(object obj)
            {
                if (Object.ReferenceEquals(this, obj))
                {
                    return true;
                }
                return (obj as ItemData).pos == pos;
            }

            public override int GetHashCode()
            {
                return pos.GetHashCode();
            }
        }

        public Bounds contentBounds
        {
            get
            {
                var b = new Bounds();
                if (startIndex >= 0 && items.Count > startIndex)
                {
                    var b1 = items[startIndex].bounds;
                    var b2 = items[endIndex].bounds;
                    if (border.y != 0)
                    {
                        var max = b1.max;
                        max.y += border.y;
                        var min = b1.min;
                        min.y -= border.w;
                        b1.SetMinMax(min, max);
                    }
                    b.SetMinMax(b2.min, b1.max);
                }
                return b;
            }
        }

        private Rect? _localClipBounds;
        public Rect localClipBounds // bottom-left and topRight
        {
            get
            {
                if (_localClipBounds == null || transform.hasChanged)
                {
                    var r = new Rect(viewRect.rect);
                    //var relative = CalculateRelativeRectTransformBounds(viewRect, content);
                    //r.center -= new Vector2(relative.min.x, relative.max.y);
                    _localClipBounds = r;
                    transform.hasChanged = false;
                }
                return _localClipBounds.Value;
            }
        }

        private Vector3[] corners;
        public Bounds CalculateRelativeRectTransformBounds(RectTransform root, RectTransform child)
        {
            if (corners == null)
            {
                corners = new Vector3[4];
            }
            Vector3 vector = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 vector2 = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            Matrix4x4 worldToLocalMatrix = root.worldToLocalMatrix;
            child.GetWorldCorners(corners);
            for (int j = 0; j < 4; j++)
            {
                Vector3 lhs = worldToLocalMatrix.MultiplyPoint3x4(corners[j]);
                vector = Vector3.Min(lhs, vector);
                vector2 = Vector3.Max(lhs, vector2);
            }
            Bounds result = new Bounds(vector, Vector3.zero);
            result.Encapsulate(vector2);
            return result;
        }

        public int contentSize => contentData?.Count ?? 0;

        /// <summary>
        /// Callback that will be called every time an item needs to have its content updated.
        /// The 'wrapIndex' is the index within the child list, and 'realIndex' is the index using position logic.
        /// </summary>

        [SerializeField] private InstantGridItem prefab;
        private bool hideInactive => true;
        [SerializeField] private Vector2Int padding = new Vector2Int(1, 1);
        [SerializeField] private Vector4Int border = new Vector4Int();
        [SerializeField] private int lineItemSize = 1;
        [SerializeField] private GameObject empty;
        [SerializeField] private bool changeItemName;
        [SerializeField] private bool alignX = true;

        private int startIndex = -1;
        private int endIndex = -1;
        private List<ItemData> items = new List<ItemData>();
        private List<InstantGridItem> children = new List<InstantGridItem>();
        private Dictionary<ItemData, InstantGridItem> visibles = new Dictionary<ItemData, InstantGridItem>();
        private Queue<InstantGridItem> available = new Queue<InstantGridItem>();

        public bool isCentered => viewRect.pivot.x == 0.5f;
        public bool isBottomPivot => viewRect.pivot.y == 0;
        public bool isLeftPivot => viewRect.pivot.x != 1;
        public bool isScrollAtBottom => visibles.Count == 0 || (endIndex == items.Count - 1 && IsVisible(endIndex));
        private float top => 0;
        private float bottom => -localClipBounds.height;

        public bool isContentFitInViewport => contentBounds.size.y < localClipBounds.height;

        public delegate void InitDelegate(InstantGridItem e, object data, int i);
        private IList contentData;
        public InitDelegate initDelegate;

        protected override void Awake()
        {
            if (!Application.isPlaying)
            {
                if (viewport == null)
                {
                    viewport = transform as RectTransform;
                    horizontal = false;
                }
            }
        }
#if UNITY_EDITOR
#endif

        private void Update()
        {
            if (Application.isPlaying && viewRect.hasChanged)
            {
                OnMove();
            }
        }

        protected virtual void OnMove()
        {
            _localClipBounds = null;
            WrapContent();
        }

        protected override void LateUpdate()
        {
            UpdateContentSize();
            base.LateUpdate();
        }

        private RectTransform contentSizeRect;
        private void UpdateContentSize()
        {
            if (Application.isPlaying && contentSizeRect == null)
            {
                var go = new GameObject("content-size", typeof(Image));
                go.layer = gameObject.layer;
                go.transform.SetParent(content, false);
                contentSizeRect = go.transform as RectTransform;
                var image = go.GetComponent<Image>();
                image.color = new Color(1, 1, 1, 0);
                contentSizeRect.sizeDelta = contentBounds.size;
            }
        }

        public bool isScrollable
        {
            get
            {
                if (contentData == null || viewport == null)
                {
                    return false;
                }
                if (startIndex > 0 || endIndex < contentData.Count - 1)
                {
                    return true;
                }
                return !isContentFitInViewport;
            }
        }

        public void Init(IList list, InitDelegate initDelegate = null, int focusIndex = -1)
        {
            contentData = list;
            if (initDelegate != null)
            {
                this.initDelegate = initDelegate;
            }
            if (!gameObject.activeInHierarchy || !enabled)
            {
                return;
            }
            UpdateItems(true, focusIndex);
        }

        public bool IsVisible(int index)
        {
            if (startIndex < 0)
            {
                return false;
            }
            var i0 = FindIndex(top);
            var i1 = FindIndex(bottom);
            return i0 <= index && index <= i1;
        }

        public void HideAll()
        {
            foreach (var c in children)
            {
                if (c.isValid)
                {
                    RemoveCell(c);
                    c.gameObject.SetActive(false);
                }
            }
            items.Clear();
            startIndex = -1;
            endIndex = -1;
        }

        public void Clear()
        {
            this.contentData = null;
            HideAll();
        }

        [ContextMenu("Scroll To Start")]
        public void ScrollToStart()
        {
            if (!isScrollable)
            {
                return;
            }
            StopMovement();
            if (vertical)
            {
                verticalNormalizedPosition = viewport.pivot.y;
            } else
            {
                horizontalNormalizedPosition = viewport.pivot.x;
            }
        }

        [ContextMenu("Scroll To End")]
        public void ScrollToEnd()
        {
            if (!isScrollable)
            {
                return;
            }
            StopMovement();
            if (vertical)
            {
                verticalNormalizedPosition = 1-viewport.pivot.y;
            }
            else
            {
                horizontalNormalizedPosition = 1-viewport.pivot.x;
            }
        }

        private void InitItem(InstantGridItem c, int i)
        {
            int iRow = i / lineItemSize;
            int iCol = i % lineItemSize;
            if (i < contentData.Count)
            {
                initDelegate?.Invoke(c, contentData[i], i);
                if (items[i] == null)
                {
                    items[i] = new ItemData(i);
                    var bound =
                        c.bound != null ?
                        new Bounds(c.rect.rect.center, c.rect.rect.size) :
                        RectTransformUtility.CalculateRelativeRectTransformBounds(c.rect);
                    var min = bound.min;
                    var max = bound.max;

                    float x = 0;
                    if (iCol == 0)
                    {
                        if (alignX)
                        {
                            var left = 0;
                            var right = localClipBounds.width;
                            x = isLeftPivot ? left - min.x + border.x : right - max.x - border.x;
                        }
                        else if (prefab != null)
                        {
                            x = prefab.rect.localPosition.x;
                        }
                        else // for editor purpose
                        {
                            x = c.rect.localPosition.x;
                        }
                    }
                    else
                    {
                        if (i == startIndex - 1)
                        {
                            var prevBound = items[startIndex].bounds;
                            x = isLeftPivot ? prevBound.min.x - padding.x : prevBound.max.x + padding.x;
                        }
                        // add to bottom
                        else if (i == endIndex + 1)
                        {
                            var prevBound = items[endIndex].bounds;
                            x = isLeftPivot ? prevBound.max.x + padding.x : prevBound.min.x - padding.x;
                        }
                    }

                    if (startIndex >= 0)
                    {
                        // add to top
                        if (i == startIndex - 1)
                        {
                            if (iCol == 0)
                            {
                                var dy = -min.y;
                                var b0 = items[startIndex].bounds;
                                var top = b0.max.y; //upper bound of previous
                                var pos = new Vector2(x, Mathf.RoundToInt(top + 1 + dy + padding.y));
                                min.x += pos.x;
                                min.y += pos.y;
                                max.x += pos.x;
                                max.y += pos.y; // move on the top of the previous
                                bound.SetMinMax(min, max);
                                items[i].pos = pos;
                            }
                            else
                            {
                                bound = items[startIndex].bounds;
                                var pos = items[startIndex].pos;
                                pos.x += items[startIndex].bounds.size.x + padding.x;
                                items[i].pos = pos;
                            }
                            startIndex = i;
                        }
                        // add to bottom
                        else if (i == endIndex + 1)
                        {
                            if (iCol == 0)
                            {
                                var dy = max.y;
                                var b0 = items[endIndex].bounds;
                                var pos = isBottomPivot?
                                    new Vector2(x, Mathf.Round(b0.max.y + 1 + dy + padding.y)): // go upward
                                    new Vector2(x, Mathf.Round(b0.min.y - 1 - dy - padding.y)); // go downward
                                min.x += pos.x;
                                min.y += pos.y;
                                max.x += pos.x;
                                max.y += pos.y;
                                bound.SetMinMax(min, max);
                                items[i].pos = pos;
                            }
                            else
                            {
                                bound = items[endIndex].bounds;
                                var pos = items[endIndex].pos;
                                var span = items[endIndex].bounds.size.x + padding.x;
                                pos.x = isLeftPivot? pos.x + span: pos.x - span;
                                items[i].pos = pos;
                            }
                            endIndex = i;
                        }
                        else
                        {
                            UnityEngine.Assertions.Assert.IsTrue(false, i.ToString());
                        }
                        items[i].bounds = bound;
                    }
                    else
                    {
                        // for the very first cell
                        var dy = isBottomPivot ? bottom - min.y : top - max.y - border.y;
                        min.x += x;
                        max.x += x;
                        min.y += dy;
                        max.y += dy;

                        bound.SetMinMax(min, max);
                        items[i].bounds = bound;
                        items[i].pos = new Vector2(Mathf.Round(x), Mathf.Round(dy));
                        startIndex = i;
                        endIndex = i;
                    }
                }
                c.item = items[i];
                c.pos = items[i].pos;
            }
            else
            {
                if (hideInactive)
                {
                    c.gameObject.SetActive(false);
                }
            }
        }

        public void Focus(int index)
        {
            throw new NotImplementedException();
            /*
            if (IsAllFitContents())
            {
                return;
            }
            StopMovement();
            if (index > endIndex || index < startIndex)
            {
                HideAll();
                Init(contentData, initDelegate, index);
            }
            else
            {
                if (IsBottomPivot)
                {
                    SetDragAmount(1, 1, false);
                }
                else
                {
                    SetDragAmount(0, 0, false);
                }
                var b = items[index].bounds;
                if (IsBottomPivot)
                {
                    var delta = items[endIndex].bounds.min.y - b.min.y + panel.baseClipRegion.w * 0.5f;
                    MoveRelative(new Vector3(0, delta, 0));
                }
                else
                {
                    var delta = items[startIndex].bounds.min.y - b.min.y;
                    MoveRelative(new Vector3(0, delta, 0));
                }
                RestrictWithinBounds(true);
            }
            */
        }

        [ContextMenu("Arrange")]
        public void ResetAndArrange()
        {
            if (viewport == null || content == null)
            {
                Debug.LogWarning("viewport or content is not assigned");
                return;
            }
            content.pivot = new Vector2(0, 1);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            var list = transform.GetComponentsInChildren<InstantGridItem>();
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
                UnityEditor.Undo.RecordObject(gameObject, "arrange");
            }
#endif
            _localClipBounds = null;
            visibles.Clear();
            available.Clear();
            children.Clear();
            startIndex = -1;
            endIndex = -1;
            items.Clear();
            Init(list, focusIndex: 0);

        }

        public void WrapContent()
        {
            UpdateItems(false, -1);
        }

        public void InsertBefore(int count)
        {
            if (contentData == null || startIndex < 0)
            {
                throw new System.Exception("Init() instead");
            }
            startIndex += count;
            endIndex += count;

            var inserts = new List<ItemData>();
            for (int i = 0; i < count; ++i)
            {
                inserts.Add(null);
            }
            items.InsertRange(0, inserts);
        }

        private void InitChildren()
        {
            if (children.Count != 0)
            {
                return;
            }

            foreach (Transform t in content)
            {
                if (t.TryGetComponent<InstantGridItem>(out var e))
                {
                    if (Application.isPlaying)
                    {
                        if (prefab == null)
                        {
                            prefab = e;
                        }
                        e.gameObject.SetActive(false);
                    }
                    if (prefab != e)
                    {
                        available.Enqueue(e);
                        children.Add(e);
                    } else
                    {
                        e.gameObject.SetActive(false);
                    }
                }
            }
            InitContentRoot();
        }

        private void InitContentRoot()
        {
            if (viewport == null || content == null)
            {
                Debug.LogWarning("viewport or content is not assigned");
                return;
            }
            content.anchorMin = Vector2.zero;
            content.anchorMax = Vector3.one;
            content.pivot = new Vector2(0, 1);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="forcedRefresh">true to update already visible item</param>
        public void UpdateItems(bool forcedRefresh, int focusIndex = -1)
        {
            if (!gameObject.activeInHierarchy || !enabled || lineItemSize <= 0)
            {
                return;
            }
            InitChildren();

            // hide out of bounds
            if (focusIndex >= 0 && !IsVisible(focusIndex))
            {
                HideAll();
            }
            else if (visibles.Count > 0)
            {
                HideCell(bottom, top);
            }

            if (contentData != null && contentData.Count > 0)
            {
                if (empty != null)
                {
                    empty.SetActive(false);
                }
                // fit storage
                if (items.Count > contentData.Count)
                {
                    items.RemoveRange(contentData.Count, items.Count - contentData.Count);
                    endIndex = Mathf.Min(endIndex, items.Count - 1);
                }
                else
                {
                    while (items.Count < contentData.Count)
                    {
                        items.Add(null);
                    }
                }

                if (visibles.Count == 0) // if no visible cells
                {
                    if (isBottomPivot)
                    {
                        var start = focusIndex >= 0 ? focusIndex : contentData.Count - 1;
                        // add from the last item
                        for (int i = start; i >= 0 && (startIndex < 0 || (items[startIndex].bounds.min.y <= top && items[startIndex].bounds.max.y >= bottom)); --i)
                        {
                            ShowCell(i, forcedRefresh);
                        }
                    }
                    else
                    {
                        var start = focusIndex >= 0 ? focusIndex : 0;
                        // add from the last item
                        for (int i = start; i < contentData.Count && (endIndex < 0 || (items[startIndex].bounds.min.y <= top && items[startIndex].bounds.max.y >= bottom)); ++i)
                        {
                            ShowCell(i, forcedRefresh);
                        }
                    }
                }
                else if (focusIndex >= 0)
                {
                    // if not yet instantiated
                    if (items[focusIndex] == null)
                    {
                        if (isBottomPivot)
                        {
                            for (int i = startIndex; i >= focusIndex; --i)
                            {
                                ShowCell(i, forcedRefresh);
                            }
                        }
                        else
                        {
                            for (int i = endIndex; i <= focusIndex; ++i)
                            {
                                ShowCell(i, forcedRefresh);
                            }
                        }
                    }
                    Focus(focusIndex);
                }
                // show inside bounds
                ShowCellsInViewport(bottom, top, forcedRefresh);
                if (!isScrollable)
                {
                    ScrollToStart();
                }
            }
            else
            {
                HideAll();
                if (empty != null)
                {
                    empty.SetActive(true);
                }
            }
        }

        private void ShowCellsInViewport(float min, float max, bool refreshData)
        {
            // Instantiate upward if not yet created.
            while (startIndex > 0 && (items[startIndex].pos.y < max || startIndex % lineItemSize != 0))
            {
                var s = startIndex;
                ShowCell(startIndex - 1, refreshData);
                UnityEngine.Assertions.Assert.AreNotEqual(startIndex, s);
            }
            // Instantiate downward if not yet created.
            while (endIndex < contentData.Count - 1 && (items[endIndex].pos.y > min || endIndex % lineItemSize != lineItemSize - 1))
            {
                var s = endIndex;
                ShowCell(endIndex + 1, refreshData);
                UnityEngine.Assertions.Assert.AreNotEqual(endIndex, s);
            }

            var i0 = FindIndex(max);
            var i1 = FindIndex(min);
            i0 -= i0 % lineItemSize;
            i1 = Mathf.Min(i1 - i1 % lineItemSize + lineItemSize - 1, contentData.Count - 1);
            for (int i = i0; i <= i1; ++i)
            {
                ShowCell(i, refreshData);
            }
            // check upper boundary
            while (i0 > 0 && (items[i0].bounds.max.y <= max || i0 % lineItemSize != 0))
            {
                --i0;
                ShowCell(i0, refreshData);
            }
            // check lower boundary
            while (i1 < contentData.Count - 1 && (items[i1].bounds.min.y >= min || i1 % lineItemSize != lineItemSize - 1))
            {
                ++i1;
                ShowCell(i1, refreshData);
            }
            Align(i0, i1);
        }

        private void Align(int i0, int i1)
        {
            // align centered
            if (viewport.pivot.x == 0.5f)
            {
                var iMin = i0;
                var min = float.MaxValue;
                var max = float.MinValue;
                for (int i= i0; i<=i1; ++i)
                {
                    if (i%lineItemSize != 0)
                    {
                        min = Mathf.Min(min, items[i].pos.x + items[i].bounds.min.x);
                        max = Mathf.Max(max, items[i].pos.x + items[i].bounds.max.x);
                    } else
                    {
                        var delta = viewport.rect.width - (max - min);
                        ShiftItem(iMin, i-1, delta*0.5f, 0);
                        iMin = i;
                        min = items[i].pos.x + items[i].bounds.min.x;
                        max = items[i].pos.x + items[i].bounds.max.x;
                    }
                }
                if (i1-iMin != lineItemSize-1)
                {
                    var delta = viewport.rect.width - (max - min);
                    ShiftItem(iMin, i1, delta*0.5f, 0);
                }
            }
        }

        private void ShiftItem(int i0, int i1, float dx, float dy)
        {
            for (int i = i0; i <= i1; ++i)
            {
                var c = visibles.Find(items[i]);
                if (c != null)
                {
                    items[i].pos = items[i].pos + new Vector2(dx, dy);
                    //items[i].bounds.center = items[i].bounds.center + new Vector3(dx, dy, 0);
                    c.pos = items[i].pos;
                }
            }
        }

        private void ShowCell(int index, bool refreshData)
        {
            var item = items[index];
            var c = visibles.Find(item);
            if (c != null)
            {
                c.pos = item.pos;
                c.gameObject.SetActive(true);
                if (refreshData)
                {
                    initDelegate?.Invoke(c, contentData[index], index);
                }
            }
            else
            {
                if (available.Count > 0)
                {
                    c = available.Dequeue();
                }
                else
                {
                    c = Instantiate(prefab, content, false);
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        UnityEditor.Undo.RegisterCreatedObjectUndo(c.gameObject, "arrange");
                    }
#endif
                    children.Add(c);
                }
                c.gameObject.SetActive(true);
                InitItem(c, index);
                visibles[items[index]] = c;
            }
            if (changeItemName)
            {
                c.name = index.ToString();
            }
        }

        private bool HideCell(float min, float max)
        {
            bool hasHidden = false;
            // hide if cell is out of bounds
            for (int i = 0, imax = children.Count; i < imax; ++i)
            {
                var c = children[i];
                if (c.isValid)
                {
                    bool outOfBounds = c.item.bounds.max.y < min || c.item.bounds.min.y > max;
                    if (c.item.index >= contentData.Count || outOfBounds)
                    {
                        RemoveCell(c);
                        if (hideInactive || !outOfBounds)
                        {
                            c.gameObject.SetActive(false);
                        }
                        hasHidden = true;
                    }
                }
            }
            return hasHidden;
        }

        private void RemoveCell(InstantGridItem c)
        {
            available.Enqueue(c);
            visibles.Remove(c.item);

            c.Clear();
        }

        private int FindIndex(float val)
        {
            var index = items.BinarySearch(startIndex, endIndex - startIndex + 1, i => Compare(i.pos.y, val));
            return Mathf.Clamp(Mathf.Abs(index), 0, contentData.Count - 1);

            int Compare(float f1, float f2)
            {
                if (f1 < f2)
                {
                    return -1;
                }
                else if (f1 > f2)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            var b = contentBounds;
            var c = Gizmos.color;
            Gizmos.color = Color.red;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(b.center, b.size);
            Gizmos.DrawWireCube(b.center, b.size + Vector3.one);
            Gizmos.DrawWireCube(b.center, b.size - Vector3.one);
            Gizmos.color = c;
        }

        public void RecalculateClip()
        {
            _localClipBounds = null;
        }
    }
}
