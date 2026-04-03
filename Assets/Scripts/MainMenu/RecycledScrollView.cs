using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A pool-based recycled scroll view for the level-select list.
/// Only instantiates enough <see cref="LevelButton"/> instances to fill the visible
/// viewport plus a small buffer; repositions and rebinds them as the user scrolls.
/// This keeps performance constant regardless of total level count.
/// </summary>
public class RecycledScrollView : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [SerializeField] private ScrollRect  scrollRect;
    [SerializeField] private LevelButton buttonPrefab;

    [Tooltip("MonoBehaviour in this scene that implements ISceneNavigator (e.g. ScenesManager).")]
    [SerializeField] private MonoBehaviour sceneNavigatorBehaviour;

    [Tooltip("Height of each level button in pixels.")]
    [SerializeField] private float itemHeight = 100f;

    [Tooltip("Vertical gap between buttons in pixels.")]
    [SerializeField] private float spacing    = 10f;

    // ── Runtime state ─────────────────────────────────────────────────────────

    private readonly List<LevelButton> pool = new List<LevelButton>();
    private ISceneNavigator sceneNavigator;
    private List<int>       data             = new List<int>();
    private RectTransform   content;
    private int             poolSize;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        sceneNavigator = sceneNavigatorBehaviour as ISceneNavigator;
        if (sceneNavigator == null)
            Debug.LogError("[RecycledScrollView] sceneNavigatorBehaviour does not implement ISceneNavigator.", this);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Populates the scroll view with the supplied level numbers.
    /// Numbers are sorted ascending; gaps in the sequence are allowed.
    /// Call this every time the level list changes.
    /// </summary>
    public void SetData(List<int> levelNumbers)
    {
        if (content == null)
            content = scrollRect.content;

        data = new List<int>(levelNumbers);
        data.Sort();

        // Resize the content to fit all items
        float stride      = itemHeight + spacing;
        float totalHeight = data.Count * stride - spacing;
        content.sizeDelta = new Vector2(content.sizeDelta.x, Mathf.Max(0f, totalHeight));

        // Calculate how many pool items are needed to cover the viewport + 1 extra row
        float viewportHeight = ((RectTransform)scrollRect.transform).rect.height;
        poolSize = Mathf.CeilToInt(viewportHeight / stride) + 2;
        poolSize = Mathf.Min(poolSize, data.Count);

        EnsurePoolSize(poolSize);

        scrollRect.onValueChanged.RemoveAllListeners();
        scrollRect.onValueChanged.AddListener(_ => RefreshVisible());

        // Reset scroll position and do the first layout pass
        content.anchoredPosition = Vector2.zero;
        RefreshVisible();
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private void EnsurePoolSize(int required)
    {
        while (pool.Count < required)
        {
            var btn = Instantiate(buttonPrefab, content);
            btn.SetNavigator(sceneNavigator);
            pool.Add(btn);
        }

        // Hide any surplus items left from a previous, larger data set
        for (int i = required; i < pool.Count; i++)
            pool[i].gameObject.SetActive(false);
    }

    private void RefreshVisible()
    {
        float stride     = itemHeight + spacing;
        float scrollY    = content.anchoredPosition.y;
        int   firstIndex = Mathf.Max(0, Mathf.FloorToInt(scrollY / stride));

        for (int i = 0; i < poolSize; i++)
        {
            int dataIndex = firstIndex + i;

            if (dataIndex >= data.Count)
            {
                pool[i].gameObject.SetActive(false);
                continue;
            }

            pool[i].gameObject.SetActive(true);
            pool[i].SetLevelText(data[dataIndex]);

            // Position: content's pivot is top-centre; y grows downward
            float y = -(dataIndex * stride + itemHeight * 0.5f);
            ((RectTransform)pool[i].transform).anchoredPosition = new Vector2(0f, y);
        }
    }
}
