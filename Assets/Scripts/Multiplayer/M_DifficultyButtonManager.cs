using UnityEngine;

public class M_DifficultyButtonManager : MonoBehaviour
{
    public static M_DifficultyButtonManager Instance { get; private set; }

    [Header("Difficulty Buttons")]
    [SerializeField] private DifficultyButton hardButton;
    [SerializeField] private DifficultyButton veryHardButton;

    private DifficultyButton currentSelected;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void SelectButton(DifficultyButton selected)
    {
        if (selected == null) return;

        if (currentSelected != null && currentSelected != selected)
            currentSelected.Deselect();

        currentSelected = selected;
        selected.Select();

        NotifyLevelSelector(selected);
    }

    private void NotifyLevelSelector(DifficultyButton selected)
    {
        MLevelSelectionManager selector = MLevelSelectionManager.Instance;
        if (selector == null) return;

        if (selected == hardButton)
            selector.OnHardSelected();
        else if (selected == veryHardButton)
            selector.OnVeryHardSelected();
    }
}
