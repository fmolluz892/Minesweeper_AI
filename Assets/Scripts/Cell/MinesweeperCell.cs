
using TMPro;
using UnityEngine;

public class MinesweeperCell : MonoBehaviour
{
    // --------------------------------------------------
    // REFERENCIAS
    // --------------------------------------------------

    [Header("Referencias visuales")]
    [SerializeField] private GameObject numberCanvas;
    [SerializeField] private TMP_Text numberText;
    [SerializeField] private GameObject mineVisual;
    [SerializeField] private GameObject flagVisual;
    [SerializeField] private Renderer cellRenderer;

    // --------------------------------------------------
    // ESTADO DEL JUEGO
    // --------------------------------------------------

    private MinesweeperBoard board;

    public int Row { get; private set; }
    public int Column { get; private set; }

    public bool IsMine { get; private set; }
    public bool IsRevealed { get; private set; }
    public bool IsFlagged { get; private set; }

    public int AdjacentMines { get; private set; }

    // --------------------------------------------------
    // INICIALIZACIÓN
    // --------------------------------------------------

    public void Initialize(MinesweeperBoard board, int row, int column)
    {
        this.board = board;

        Row = row;
        Column = column;

        IsMine = false;
        IsRevealed = false;
        IsFlagged = false;

        AdjacentMines = 0;

        ResetVisuals();
    }

    private void ResetVisuals()
    {
        if (numberCanvas != null)
            numberCanvas.SetActive(false);

        if (mineVisual != null)
            mineVisual.SetActive(false);

        if (flagVisual != null)
            flagVisual.SetActive(false);

        if (numberText != null)
            numberText.text = "";

        if (cellRenderer != null)
            cellRenderer.material.color = Color.white;
    }

    // --------------------------------------------------
    // MODIFICACIÓN DEL ESTADO
    // --------------------------------------------------

    public void SetMine()
    {
        IsMine = true;
    }

    public void SetAdjacentMines(int count)
    {
        if (count < 0 || count > 8)
        {
            Debug.LogError("Número de minas adyacentes no válido.");
            return;
        }
        AdjacentMines = count;
    }

    public void SetRevealed()
    {
        IsRevealed = true;
    }

    public void SetFlagged(bool flagged)
    {
        IsFlagged = flagged;
        ShowFlag(flagged);
    }

    // --------------------------------------------------
    // INTERACCIÓN
    // --------------------------------------------------

    public void Interact()
    {
        if (board == null || IsRevealed || IsFlagged)
            return;

        board.TryRevealCellFromPlayer(Row, Column);
    }

    public void Flag()
    {
        if (board == null || IsRevealed)
            return;

        board.TryFlagCellFromPlayer(Row, Column);
    }

    // --------------------------------------------------
    // REPRESENTACIÓN VISUAL
    // --------------------------------------------------

    public void RevealVisual()
    {
        if (IsMine)
        {
            if (mineVisual != null)
                mineVisual.SetActive(true);

            if (cellRenderer != null)
                cellRenderer.material.color =
                    new Color(0.5f, 0.5f, 0.5f);

            return;
        }

        if (AdjacentMines > 0)
            ShowNumber();

        if (cellRenderer != null)
            cellRenderer.material.color =
                new Color(0.9f, 0.9f, 0.9f);
    }

    private void ShowNumber()
    {
        if (numberText != null)
            numberText.text = AdjacentMines.ToString();

        if (numberCanvas != null)
            numberCanvas.SetActive(true);
    }

    public void ShowFlag(bool visible)
    {
        if (flagVisual != null)
            flagVisual.SetActive(visible);
    }
}

