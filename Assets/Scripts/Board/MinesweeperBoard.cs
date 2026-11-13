using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.CullingGroup;
using Random = UnityEngine.Random;

public enum MinesweeperState
{
    NotStarted,
    Playing,
    Won,
    Lost
}



public class MinesweeperBoard : MonoBehaviour
{

    public event Action<MinesweeperState> StateChanged;
    
    // Evento para control de n_minas, banderas colocadas y minas_restantes
    public event Action<int, int, int> CountersChanged;

    [Header("Configuración del tablero")]
    [SerializeField] private int rows = 10;
    [SerializeField] private int columns = 10;
    [SerializeField] private int mineCount = 25;

    [Header("Celdas")]
    [SerializeField] private MinesweeperCell cellPrefab;
    [SerializeField] private float cellSize = 1f;

    private MinesweeperCell[,] cells;

    private int flagCount;
    private bool playerCanInteract;

    public float CellSize => cellSize; 
    public int Rows => rows;
    public int Columns => columns;
    public int MineCount => mineCount;
    public int FlagCount => flagCount;
    public int RemainingFlags => mineCount - flagCount;

    public MinesweeperState State { get; private set; }
        = MinesweeperState.NotStarted;


    // INICIALIZACIÓN DEL TABLERO

    public bool Initialize(int newRows, int newColumns, int newMineCount)
    {
        if (newRows < 1 || newColumns < 1)
        {
            Debug.LogError("Las filas y columnas deben ser mayores que cero.");
            return false;
        }

        if (newMineCount < 0 ||
            (long)newMineCount >= (long)newRows * newColumns)
        {
            Debug.LogError(
                "El número de minas debe ser mayor o igual que cero " +
                "y menor que el número de celdas."
            );
            return false;
        }

        if (cellPrefab == null)
        {
            Debug.LogError(
                "No se ha asignado el prefab de MinesweeperCell."
            );
            return false;
        }

        // Guardamos la configuración elegida.
        rows = newRows;
        columns = newColumns;
        mineCount = newMineCount;

        // Reiniciamos los datos de la partida.
        flagCount = 0;
        playerCanInteract = false;
        ChangeState(MinesweeperState.Playing);

        // Reiniciamos los contadores visuales
        CountersChanged?.Invoke(
            mineCount,
            flagCount,
            RemainingFlags
         );

        ClearBoard();
        GenerateBoard();
        PlaceMines();
        CalculateAdjacentMines();

        Debug.Log(
            $"Tablero creado: {rows} filas, {columns} columnas, " +
            $"{mineCount} minas."
        );


        return true;
    }


    // GENERACIÓN DEL TABLERO

    private void GenerateBoard()
    {
        cells = new MinesweeperCell[rows, columns];

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                Vector3 position = transform.position + new Vector3(
                    column * cellSize,
                    row * cellSize,
                    0f
                );

                MinesweeperCell cell = Instantiate(
                    cellPrefab,
                    position,
                    Quaternion.identity,
                    transform
                );

                cell.Initialize(this, row, column);
                cells[row, column] = cell;
            }
        }
    }


    private void ClearBoard()
    {
        // Eliminamos las celdas de la partida anterior.
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;

            child.SetActive(false);
            Destroy(child);
        }

        cells = null;
    }


    // COLOCACIÓN ALEATORIA DE MINAS

    private void PlaceMines()
    {
        List<MinesweeperCell> availableCells =
            new List<MinesweeperCell>();

        // Añadimos todas las celdas a la lista.
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                availableCells.Add(cells[row, column]);
            }
        }

        // Algoritmo de barajado Fisher-Yates.
        for (int i = availableCells.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);

            (availableCells[i], availableCells[j]) =
                (availableCells[j], availableCells[i]);
        }

        // Colocamos las minas en las primeras posiciones.
        for (int i = 0; i < mineCount; i++)
        {
            availableCells[i].SetMine();
        }
    }


    // CÁLCULO DE MINAS ADYACENTES

    private void CalculateAdjacentMines()
    {
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                if (cells[row, column].IsMine)
                    continue;

                int adjacentMines = CountAdjacentMines(row, column);

                cells[row, column].SetAdjacentMines(adjacentMines);
            }
        }
    }


    private int CountAdjacentMines(int row, int column)
    {
        int count = 0;

        for (int rowOffset = -1; rowOffset <= 1; rowOffset++)
        {
            for (int columnOffset = -1; columnOffset <= 1; columnOffset++)
            {
                if (rowOffset == 0 && columnOffset == 0)
                    continue;

                int neighbourRow = row + rowOffset;
                int neighbourColumn = column + columnOffset;

                MinesweeperCell neighbour =
                    GetCell(neighbourRow, neighbourColumn);

                if (neighbour != null && neighbour.IsMine)
                {
                    count++;
                }
            }
        }

        return count;
    }


    // REVELACIÓN DE CELDAS

    // Este método será utilizado por el bot.
    public bool RevealCell(int row, int column)
    {
        if (State != MinesweeperState.Playing)
            return false;

        bool revealed = RevealCellInternal(row, column);

        if (revealed && State == MinesweeperState.Playing)
        {
            CheckVictory();
        }

        return revealed;
    }


    // Método interno que permite revelar zonas vacías recursivamente.
    private bool RevealCellInternal(int row, int column)
    {
        MinesweeperCell cell = GetCell(row, column);

        if (cell == null || cell.IsRevealed || cell.IsFlagged)
            return false;

        cell.SetRevealed();
        cell.RevealVisual();

        // Si encontramos una mina, termina la partida.
        if (cell.IsMine)
        {
            ChangeState(MinesweeperState.Lost);
            RevealAllMines();

            Debug.Log("¡DERROTA! Has encontrado una mina.");
            return true;
        }

        // Si no tiene minas alrededor, revelamos sus vecinos.
        if (cell.AdjacentMines == 0)
        {
            for (int rowOffset = -1; rowOffset <= 1; rowOffset++)
            {
                for (int columnOffset = -1; columnOffset <= 1;
                     columnOffset++)
                {
                    if (rowOffset == 0 && columnOffset == 0)
                        continue;

                    RevealCellInternal(
                        row + rowOffset,
                        column + columnOffset
                    );

                    if (State == MinesweeperState.Lost)
                        return true;
                }
            }
        }

        return true;
    }


    private void RevealAllMines()
    {
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                MinesweeperCell cell = cells[row, column];

                if (cell.IsMine && !cell.IsRevealed)
                {
                    cell.SetRevealed();
                    cell.RevealVisual();
                }
            }
        }
    }


    // COLOCACIÓN Y RETIRADA DE BANDERAS

    // Este método será utilizado por el bot.
    public bool FlagCell(int row, int column)
    {
        if (State != MinesweeperState.Playing)
            return false;

        MinesweeperCell cell = GetCell(row, column);

        if (cell == null || cell.IsRevealed)
            return false;

        if (cell.IsFlagged)
        {
            cell.SetFlagged(false);
            flagCount--;

            CountersChanged?.Invoke(
                mineCount,
                flagCount,
                RemainingFlags
            );

            return true;
        }

        if (flagCount >= mineCount)
            return false;

        cell.SetFlagged(true);
        flagCount++;

        CountersChanged?.Invoke(
            mineCount,
            flagCount,
            RemainingFlags
        );

        return true;
    }


    // CONTROL DE INTERACCIÓN DEL JUGADOR

    public void SetPlayerInteractionEnabled(bool enabled)
    {
        playerCanInteract = enabled;
    }


    public bool TryRevealCellFromPlayer(int row, int column)
    {
        if (!playerCanInteract)
            return false;

        return RevealCell(row, column);
    }


    public bool TryFlagCellFromPlayer(int row, int column)
    {
        if (!playerCanInteract)
            return false;

        return FlagCell(row, column);
    }


    // CONTROL DE VICTORIA

    private void CheckVictory()
    {
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                MinesweeperCell cell = cells[row, column];

                if (!cell.IsMine && !cell.IsRevealed)
                    return;
            }
        }

        ChangeState(MinesweeperState.Won);
        Debug.Log("¡VICTORIA! Has descubierto todas las celdas seguras.");
    }


    // ACCESO A LAS CELDAS

    public MinesweeperCell GetCell(int row, int column)
    {
        if (cells == null ||
            row < 0 || row >= rows ||
            column < 0 || column >= columns)
        {
            return null;
        }

        return cells[row, column];
    }


    // CONTROL DE ESTADO DE JUEGO

    private void ChangeState(MinesweeperState newState)
    {
        if (State == newState)
            return;

        State = newState;

        if (State == MinesweeperState.Won ||
            State == MinesweeperState.Lost)
        {
            playerCanInteract = false;
        }

        StateChanged?.Invoke(State);
    }


}
