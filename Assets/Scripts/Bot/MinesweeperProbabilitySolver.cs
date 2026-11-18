using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class MinesweeperProbabilitySolver : MonoBehaviour
{
    // Número máximo de variables frontera que vamos a explorar.
    private const int MaxFrontierCells = 28;

    // Límite de nodos para evitar búsquedas demasiado largas
    private const int MaxSearchNodes = 5000000;

    private class Constraint
    {
        public int[] Variables;
        public int Target;
    }

    private List<Vector2Int> frontierCells;
    private List<Constraint> constraints;

    private List<List<Vector2Int>> rawPositions = new List<List<Vector2Int>>();

    private int[] assignments;
    private double[] mineWeights;

    private int remainingMines;
    private int outsideCellCount;

    private double totalWeight;
    private double outsideMineWeight;

    private int searchNodes;
    private bool searchLimitReached;


    // MÉTODO PRINCIPAL

    public bool TryGetSafestCell(MinesweeperBoard board, out Vector2Int safestCell, out double safestProbability)
    {
        // Inicializamos valores
        safestCell = Vector2Int.zero;
        safestProbability = 1.0;

        frontierCells = new List<Vector2Int>();
        constraints = new List<Constraint>();
        remainingMines = board.RemainingFlags;

        if (!BuildConstraints(board))
            return false;

        outsideCellCount = CountAvailableCells(board) - frontierCells.Count;

        if (outsideCellCount < 0)
            return false;

        if (frontierCells.Count > MaxFrontierCells)
        {
            Debug.Log("La frontera es demasiado grande para el algoritmo");
            return false;
        }

        assignments = new int[frontierCells.Count];

        for (int i = 0; i < assignments.Length; i++)
            assignments[i] = -1;

        mineWeights = new double[frontierCells.Count];

        totalWeight = 0;
        outsideMineWeight = 0;

        searchNodes = 0;
        searchLimitReached = false;

        SearchConfigurations(0, 0);

        if (searchLimitReached || totalWeight <= 0)
            return false;

        // Calculamos la probabilidad de cada celda de frontera
        Dictionary<Vector2Int, double> probabilities = new Dictionary<Vector2Int, double>();

        for (int i = 0; i < frontierCells.Count; i++)
        {
            probabilities[frontierCells[i]] = mineWeights[i] / totalWeight;
        }

        // Probabilidad de las celdas que no pertenecen a la frontera
        double outsideProbability = 0;

        if(outsideCellCount > 0)
        {
            outsideProbability = outsideMineWeight / totalWeight;
        }

        // Buscamos la celda con menor probabilidad
        List<Vector2Int> bestCells = new List<Vector2Int>();

        double bestProbability = double.MaxValue;

        for(int row = 0; row < board.Rows; row++)
        {
            for (int column = 0; column < board.Columns; column++) 
            { 
                MinesweeperCell cell = board.GetCell(row, column);

                if (cell == null || cell.IsRevealed || cell.IsFlagged)
                    continue;

                Vector2Int position = new Vector2Int(row, column);

                double probability = 0;

                if(probabilities.TryGetValue(position, out double frontierProbability))
                {
                    probability = frontierProbability;
                }
                else
                {
                    probability = outsideProbability;
                }

                if(probability < bestProbability - 0.000000001)
                {
                    bestProbability = probability;
                    bestCells.Clear();
                    bestCells.Add(position);
                }
                else if (System.Math.Abs(probability - bestProbability) < 0.000000001)
                {
                    bestCells.Add(position);
                }            
            }
        }

        if(bestCells.Count == 0)
        {
            return false;
        } 

        // Desempate aleatorio entre celdas del mismo riesgo
        safestCell = bestCells[Random.Range(0, bestCells.Count)];

        safestProbability = bestProbability;

        return true;
    }


    // Búsqueda por Backtracking
    private void SearchConfigurations(int index, int assignedMines)
    {
        if (searchLimitReached)
            return;

        searchNodes++;

        if(searchNodes > MaxSearchNodes)
        {
            searchLimitReached = true;
            return;
        }

        int unassigned = assignments.Length - index;

        // Comprobación global del número de minas
        if (assignedMines > remainingMines)
            return;

        if (assignedMines + unassigned + outsideCellCount < remainingMines)
            return;

        // Comprobamos las restricciones
        if (!AllConstraintPossible(index))
            return;

        // Hemos asignado todas las celdas frontra
        if(index == assignments.Length)
        {
            RegisterConfiguration(assignedMines);
            return;
        }

        // Probamos primero que la celda sea segura
        assignments[index] = 0;

        SearchConfigurations(index + 1, assignedMines);

        // Probamos que la celda tenga una mina
        assignments[index] = 1;

        SearchConfigurations(index + 1, assignedMines +1);

        assignments[index] = -1;
    }

    private bool AllConstraintPossible(int assignedCount)
    {
        foreach(Constraint constraint in constraints)
        {
            int minesFound  = 0;
            int unknown = 0;

            foreach(int variable in constraint.Variables)
            {
                if(variable >= assignedCount)
                    unknown++;
                else if (assignments[variable] == 1)
                {
                    minesFound++;
                }
            }

            if (minesFound > constraint.Target)
                return false;

            if (minesFound + unknown < constraint.Target)
                return false;
        }

        return true;
    }

    private bool BuildConstraints(MinesweeperBoard board)
    {
        rawPositions.Clear();

        HashSet<Vector2Int> frontier = new HashSet<Vector2Int>();    

        List<Constraint> rawConstraints = new List<Constraint>();

        for (int row = 0; row < board.Rows; row++)
        {
            for(int column = 0; column < board.Columns; column++)
            {
                MinesweeperCell cell = board.GetCell(row, column);

                if (cell == null || !cell.IsRevealed)
                    continue;

                List<Vector2Int> hiddenNeighbours = new List<Vector2Int>();

                int flaggedNeighbours = 0;

                for (int dr = -1; dr <= 1; dr++)
                {
                    for (int dc = -1; dc >= 1; dc++)
                    {
                        if (dr == 0 && dc == 0)
                            continue;

                        int nr = row + dr;
                        int nc = column + dc;

                        MinesweeperCell neighbourCell = board.GetCell(nr, nc);

                        if (neighbourCell == null)
                            continue;

                        if (neighbourCell.IsFlagged)
                        {
                            flaggedNeighbours++;
                        }
                        else if (!neighbourCell.IsRevealed)
                        {
                            Vector2Int position = new Vector2Int(nr, nc);
                            hiddenNeighbours.Add(position);
                            frontier.Add(position);
                        }
                    }
                }

                int remaining = cell.AdjacentMines - flaggedNeighbours;

                // Detección de restricciones imposibles
                if (remaining < 0 || remaining > hiddenNeighbours.Count)
                {
                    return false;
                }

                if (hiddenNeighbours.Count == 0)
                    continue;

                rawConstraints.Add(new Constraint
                {
                    Variables = null,
                    Target = remaining
                });

                rawPositions.Add(hiddenNeighbours);
            }
        }

        frontierCells = new List<Vector2Int>(frontier);

        Dictionary<Vector2Int, int> indexes = new Dictionary<Vector2Int, int>();

        for(int i = 0; i< frontierCells.Count; i++)
        {
            indexes[frontierCells[i]] = i;
        }

        constraints.Clear();

        for(int i = 0; i < rawConstraints.Count; i++)
        {
            List<Vector2Int> positions = rawPositions[i];

            int[] variables = new int[positions.Count];

            for (int j = 0; j < positions.Count; j++)
                variables[j] = indexes[positions[j]];

            constraints.Add(new Constraint
            {
                Variables = variables,
                Target = rawConstraints[i].Target

            });
        }

        rawPositions.Clear();

        return true;
    }

    private void RegisterConfiguration(int frontierMines)
    {
        int neededOutside =
            remainingMines - frontierMines;

        if (neededOutside < 0 ||
            neededOutside > outsideCellCount)
            return;

        // Número de formas de distribuir las minas restantes
        // entre las celdas que no están en la frontera.
        double ways = Combination(
            outsideCellCount,
            neededOutside
        );

        if (ways <= 0)
            return;

        totalWeight += ways;

        for (int i = 0; i < assignments.Length; i++)
        {
            if (assignments[i] == 1)
                mineWeights[i] += ways;
        }

        // Formas en las que una celda concreta de fuera
        // de la frontera contiene una mina.
        if (outsideCellCount > 0 && neededOutside > 0)
        {
            outsideMineWeight += Combination(
                outsideCellCount - 1,
                neededOutside - 1
            );
        }
    }

    // --------------------------------------------------
    // COMBINACIONES MATEMÁTICAS
    // --------------------------------------------------

    private double Combination(int n, int k)
    {
        if (k < 0 || k > n)
            return 0;

        if (k == 0 || k == n)
            return 1;

        k = System.Math.Min(k, n - k);

        double result = 1;

        for (int i = 1; i <= k; i++)
        {
            result *= (double)(n - k + i) / i;
        }

        return result;
    }

    // --------------------------------------------------
    // CONTAR CELDAS DISPONIBLES
    // --------------------------------------------------

    private int CountAvailableCells(MinesweeperBoard board)
    {
        int count = 0;

        for (int row = 0; row < board.Rows; row++)
        {
            for (int column = 0; column < board.Columns; column++)
            {
                MinesweeperCell cell =
                    board.GetCell(row, column);

                if (cell != null &&
                    !cell.IsRevealed &&
                    !cell.IsFlagged)
                {
                    count++;
                }
            }
        }

        return count;
    }

}
