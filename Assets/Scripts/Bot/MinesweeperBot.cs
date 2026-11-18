
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public enum BotDifficulty
{
    Random,
    Logical,
    Probabilistic
}

public class MinesweeperBot : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private MinesweeperBoard board;

    [Header("Dificultad")]
    [SerializeField] private BotDifficulty difficulty = BotDifficulty.Random;

    [Header("Configuración del bot")]
    [SerializeField] private float delayBetweenMoves = 0.75f;

    private Coroutine botCoroutine;
    private MinesweeperProbabilitySolver probabilitySolver = new MinesweeperProbabilitySolver();

    
    // Inicia la ejecución automática.
    public void StartBot()
    {
        StopBot();

        if (board == null)
        {
            Debug.LogError("No se ha asignado MinesweeperBoard al bot.");
            return;
        }

        if (board.State != MinesweeperState.Playing)
            return;

        botCoroutine = StartCoroutine(PlayGame());
    }

    // Detiene la ejecución automática.
    public void StopBot()
    {
        if (botCoroutine != null)
        {
            StopCoroutine(botCoroutine);
            botCoroutine = null;
        }
    }

    // Ciclo de juego del bot aleatorio.

    private IEnumerator PlayGame()
    {
        while (board.State == MinesweeperState.Playing)
        {
            yield return new WaitForSeconds(delayBetweenMoves);

            if (board.State != MinesweeperState.Playing)
                break;

            bool actionPerformed = false;

            // Nivel 1: selección completamente aleatoria.
            if (difficulty == BotDifficulty.Random)
            {
                actionPerformed = PlayRandomMove();
            }

            // Nivel 2: razonamiento lógico.
            else if (difficulty == BotDifficulty.Logical)
            {
                actionPerformed = PlayLogicalMove();
            }
            else if (difficulty == BotDifficulty.Probabilistic)
            {
                actionPerformed = PlayProbabilisticMove();
            }

            if (!actionPerformed)
            {
                Debug.Log("El bot no encuentra más movimientos.");
                break;
            }
        }

        botCoroutine = null;
    }

   


    // Juego Aleatorio
    private bool PlayRandomMove()
    {
        List<Vector2Int> availableCells = GetAvailableCells();

        if (availableCells.Count == 0)
            return false;

        int randomIndex = Random.Range(0, availableCells.Count);

        Vector2Int selectedCell = availableCells[randomIndex];

        Debug.Log(
            $"Bot aleatorio: fila {selectedCell.x}, " +
            $"columna {selectedCell.y}"
        );

        return board.RevealCell(
            selectedCell.x,
            selectedCell.y
        );
    }




    // Juego Basado en Reglas
    private bool PlayLogicalMove()
    {
        HashSet<Vector2Int> safeCells = new HashSet<Vector2Int>();
        HashSet<Vector2Int> mineCells = new HashSet<Vector2Int>();

        // Analizamos todas las celdas descubiertas.
        for (int row = 0; row < board.Rows; row++)
        {
            for (int column = 0; column < board.Columns; column++)
            {
                MinesweeperCell cell = board.GetCell(row, column);

                if (cell == null || !cell.IsRevealed)
                    continue;

                // Solo analizamos celdas con números.
                if (cell.AdjacentMines <= 0)
                    continue;

                List<Vector2Int> hiddenNeighbours =
                    new List<Vector2Int>();

                int flaggedNeighbours = 0;

                // Examinamos sus ocho vecinas.
                for (int dr = -1; dr <= 1; dr++)
                {
                    for (int dc = -1; dc <= 1; dc++)
                    {
                        if (dr == 0 && dc == 0)
                            continue;

                        int neighbourRow = row + dr;
                        int neighbourColumn = column + dc;

                        MinesweeperCell neighbour =
                            board.GetCell(neighbourRow, neighbourColumn);

                        if (neighbour == null)
                            continue;

                        if (neighbour.IsFlagged)
                        {
                            flaggedNeighbours++;
                        }
                        else if (!neighbour.IsRevealed)
                        {
                            hiddenNeighbours.Add(
                                new Vector2Int(
                                    neighbourRow,
                                    neighbourColumn
                                )
                            );
                        }
                    }
                }

                int remainingMines =
                    cell.AdjacentMines - flaggedNeighbours;

                // Evitamos deducciones ante información inconsistente.
                if (remainingMines < 0 ||
                    remainingMines > hiddenNeighbours.Count)
                    continue;

                // Regla 1: todas las vecinas ocultas son seguras.
                if (remainingMines == 0)
                {
                    foreach (Vector2Int position in hiddenNeighbours)
                        safeCells.Add(position);
                }

                // Regla 2: todas las vecinas ocultas son minas.
                else if (remainingMines == hiddenNeighbours.Count)
                {
                    foreach (Vector2Int position in hiddenNeighbours)
                        mineCells.Add(position);
                }
            }
        }

        // Primero ejecutamos las deducciones de minas.
        foreach (Vector2Int position in mineCells)
        {
            Debug.Log(
                $"Bot lógico: mina deducida en " +
                $"fila {position.x}, columna {position.y}"
            );

            return board.FlagCell(position.x, position.y);
        }

        // Después descubrimos una celda que sabemos que es segura.
        foreach (Vector2Int position in safeCells)
        {
            Debug.Log(
                $"Bot lógico: celda segura en " +
                $"fila {position.x}, columna {position.y}"
            );

            return board.RevealCell(position.x, position.y);
        }

        // Si no encuentra ninguna deducción, recurre al azar.
        Debug.Log("Bot lógico: no hay deducciones. Movimiento aleatorio.");

        return PlayRandomMove();
    }



    // Obtiene las celdas que el bot todavía puede descubrir.
    private List<Vector2Int> GetAvailableCells()
    {
        List<Vector2Int> availableCells = new List<Vector2Int>();

        for (int row = 0; row < board.Rows; row++)
        {
            for (int column = 0; column < board.Columns; column++)
            {
                MinesweeperCell cell = board.GetCell(row, column);

                if (cell != null &&
                    !cell.IsRevealed &&
                    !cell.IsFlagged)
                {
                    availableCells.Add(
                        new Vector2Int(row, column)
                    );
                }
            }
        }

        return availableCells;
    }

    private void OnDisable()
    {
        StopBot();
    }

    //Método para establecer la dificultad
    public void SetDifficulty(BotDifficulty newDifficulty)
    {
        difficulty = newDifficulty;
    }


    // Juevo Basado en Probabilidades
    private bool PlayProbabilisticMove()
    {
        bool foundCell = probabilitySolver.TryGetSafestCell(
                board,
                out Vector2Int selectedCell,
                out double probability
                );

        if (!foundCell)
        {
            Debug.Log("No he completado el análisis. Movimiento aleatorio");

            return PlayRandomMove();
        }

        Debug.Log(
        $"Bot probabilístico: fila {selectedCell.x}, " +
        $"columna {selectedCell.y}. " +
        $"Riesgo estimado: {probability:P2}"
        );
        // Seguridad de Mina

        if (probability >= 0.99999)
        {
            return board.FlagCell(selectedCell.x, selectedCell.y);
        }

        return board.RevealCell(selectedCell.x, selectedCell.y);

    }


}

