
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class MinesweeperCameraController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private MinesweeperBoard board;
    [SerializeField] private Camera targetCamera;

    [Header("Configuración")]
    [SerializeField] private float padding = 1.4f;

    private int previousWidth;
    private int previousHeight;
    private int previousRows;
    private int previousColumns;

    private float previousCellSize;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (board == null || targetCamera == null)
            return;

        // Comprobamos si ha cambiado la pantalla
        // o las dimensiones del tablero.
        bool screenChanged =
            Screen.width != previousWidth ||
            Screen.height != previousHeight;

        bool boardChanged =
            board.Rows != previousRows ||
            board.Columns != previousColumns ||
            !Mathf.Approximately(
                board.CellSize, previousCellSize);

        if (screenChanged || boardChanged)
        {
            FitCamera();
        }
    }

    private void FitCamera()
    {
        targetCamera.orthographic = true;

        // Centro del tablero.
        Vector3 boardCenter = board.transform.position;

        boardCenter.x +=
            (board.Columns - 1) * board.CellSize / 2f;

        boardCenter.y +=
            (board.Rows - 1) * board.CellSize / 2f;

        // Centramos la cámara sin modificar su altura Z.
        transform.position = new Vector3(
            boardCenter.x,
            boardCenter.y,
            transform.position.z
        );

        // Calculamos el espacio necesario para mostrar
        // todo el tablero según las proporciones de pantalla.
        float boardWidth = board.Columns * board.CellSize;
        float boardHeight = board.Rows * board.CellSize;

        float aspectRatio = targetCamera.aspect;

        float verticalSize = boardHeight / 2f;
        float horizontalSize = boardWidth / (2f * aspectRatio);

        targetCamera.orthographicSize =
            Mathf.Max(verticalSize, horizontalSize) * padding;

        // Guardamos los valores actuales.
        previousWidth = Screen.width;
        previousHeight = Screen.height;
        previousRows = board.Rows;
        previousColumns = board.Columns;
        previousCellSize = board.CellSize;
    }
}

