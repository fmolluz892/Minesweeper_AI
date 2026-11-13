using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MinesweeperGame : MonoBehaviour
{
    public enum GameMode
    {
        Player,
        Bot
    }

    [Header("Referencias al tablero")]
    [SerializeField] private MinesweeperBoard board;

    [Header("Configuración del menú")]
    [SerializeField] private TMP_InputField rowsInput;
    [SerializeField] private TMP_InputField columnsInput;
    [SerializeField] private TMP_InputField minesInput;
    [SerializeField] private TMP_Dropdown modeDropdown;
    [SerializeField] private TMP_Dropdown difficultyDropdown;
    [SerializeField] private GameObject difficultyPanel;

    [Header("Paneles")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject gamePanel;
    [SerializeField] private GameObject botPanel;

    [Header("Interfaz de partida")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button backToMenuButton;

    [Header("Entrada del jugador")]
    [SerializeField] private MinesweeperInput minesweeperInput;

    [Header("Bot")]
    [SerializeField] private MinesweeperBot minesweeperBot;

    [Header("Mensajes")]
    [SerializeField] private TMP_Text menuStatusText;
    [SerializeField] private TMP_Text statusText;

    [Header("Contadores")]
    [SerializeField] private TMP_Text mineCountText;
    [SerializeField] private TMP_Text flagCountText;
    [SerializeField] private TMP_Text remainingFlagsText;


    public GameMode CurrentMode { get; private set; }

    private void Awake()
    {
        // Suscribimos al evento
        board.StateChanged += OnBoardStateChanged;
        board.CountersChanged += OnCountersChanged;

        // Conectamos los botones desde código.
        playButton.onClick.AddListener(StartGame);
        backToMenuButton.onClick.AddListener(ReturnToMenu);

        // Valores iniciales del menú.
        rowsInput.text = "10";
        columnsInput.text = "10";
        minesInput.text = "15";

        // Índice 0: jugador. Índice 1: bot.
        modeDropdown.ClearOptions();
        modeDropdown.AddOptions(
            new System.Collections.Generic.List<string>
            {
                "Jugador",
                "Bot"
            }
        );

        modeDropdown.value = 0;
        modeDropdown.RefreshShownValue();

        // Configuración de los niveles de dificultad.
        difficultyDropdown.ClearOptions();

        difficultyDropdown.AddOptions(
            new System.Collections.Generic.List<string>
            {
        "Nivel 1 - Aleatorio",
        "Nivel 2 - Lógico"
            }
        );

        difficultyDropdown.value = 0;
        difficultyDropdown.RefreshShownValue();

        // Actualizamos la visibilidad según el modo elegido.
        modeDropdown.onValueChanged.AddListener(OnModeChanged);
        OnModeChanged(modeDropdown.value);


        if (menuStatusText != null)
            menuStatusText.text = "";

        ShowMenu();
    }

    private void OnDestroy()
    {
        // Evitamos mantener listeners innecesarios.
        if (playButton != null)
            playButton.onClick.RemoveListener(StartGame);

        if (backToMenuButton != null)
            backToMenuButton.onClick.RemoveListener(ReturnToMenu);

        if (board != null)
        {
            board.StateChanged -= OnBoardStateChanged;
            board.CountersChanged -= OnCountersChanged;
        }

        if (modeDropdown != null)
            modeDropdown.onValueChanged.RemoveListener(OnModeChanged);


    }

    public void StartGame()
    {
        // Activamos el botón de volver atrás
        backToMenuButton.gameObject.SetActive(true);

        // Paramos el bot para el inicio
        if (minesweeperBot != null)
            minesweeperBot.StopBot();

        // Leemos y validamos la configuración.
        if (!int.TryParse(rowsInput.text, out int rows) ||
            !int.TryParse(columnsInput.text, out int columns) ||
            !int.TryParse(minesInput.text, out int mines))
        {
            ShowStatus("Introduce números enteros válidos.");
            return;
        }

        if (rows < 1 || columns < 1)
        {
            ShowStatus("Las filas y columnas deben ser mayores que cero.");
            return;
        }

        long totalCells = (long)rows * columns;

        if (mines < 0 || mines >= totalCells)
        {
            ShowStatus(
                "Las minas deben ser como mínimo 0 y menores " +
                "que el número total de celdas."
            );
            return;
        }

        // Creamos el tablero con los valores elegidos.
        if (!board.Initialize(rows, columns, mines))
        {
            ShowStatus("No se ha podido crear el tablero. Revisa la consola.");
            return;
        }

        // Elegimos el modo.
        CurrentMode = modeDropdown.value == 1
            ? GameMode.Bot
            : GameMode.Player;

        bool isPlayerMode = CurrentMode == GameMode.Player;

        // Activamos o desactivamos la interacción del jugador en el tablero.
        board.SetPlayerInteractionEnabled(isPlayerMode);

        // Solo el jugador humano recibe clics.
        if (minesweeperInput != null)
            minesweeperInput.enabled = isPlayerMode;

        // Mostramos la interfaz correspondiente.
        mainMenuPanel.SetActive(false);
        gamePanel.SetActive(true);
        botPanel.SetActive(!isPlayerMode);

        if (statusText != null)
        {
            statusText.text = isPlayerMode
                ? "Turno del jugador"
                : "Modo bot seleccionado";
        }

        if (CurrentMode == GameMode.Bot)
        {
            if (minesweeperBot != null)
            {
                BotDifficulty selectedDifficulty =
                    difficultyDropdown.value == 0
                        ? BotDifficulty.Random
                        : BotDifficulty.Logical;

                minesweeperBot.SetDifficulty(selectedDifficulty);

                minesweeperBot.StartBot();
            }
            else
            {
                Debug.LogError("No se ha asignado el bot en el Inspector.");
            }
        }
    }

    public void ReturnToMenu()
    {

        // Paramos el Bot
        if (minesweeperBot != null)
            minesweeperBot.StopBot();

        // Desactivamos la interacción del jugador.
        board.SetPlayerInteractionEnabled(false);

        if (minesweeperInput != null)
            minesweeperInput.enabled = false;

        // Ocultamos la partida.
        gamePanel.SetActive(false);
        botPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
        backToMenuButton.gameObject.SetActive(false);
    }

    private void ShowMenu()
    {
        // Desactivamos el botón de volver
        backToMenuButton.gameObject.SetActive(false);
       
        
        board.SetPlayerInteractionEnabled(false);

        mainMenuPanel.SetActive(true);
        gamePanel.SetActive(false);
        botPanel.SetActive(false);

        if (minesweeperInput != null)
            minesweeperInput.enabled = false;
    }

    private void ShowStatus(string message)
    {
        if (menuStatusText != null)
            menuStatusText.text = message;

        Debug.LogWarning(message);
    }


    // EVENTOS
    private void OnBoardStateChanged(MinesweeperState newState)
    {

        if (newState == MinesweeperState.Won ||
            newState == MinesweeperState.Lost)
        {
            if (minesweeperBot != null)
                minesweeperBot.StopBot();
        }
        
        if (statusText == null)
            return;

        switch (newState)
        {
            case MinesweeperState.Playing:

                statusText.text = CurrentMode == GameMode.Player
                    ? "Turno del jugador"
                    : "Turno del bot";

                break;

            case MinesweeperState.Won:

                statusText.text = "¡VICTORIA! Has ganado la partida.";

                if (minesweeperInput != null)
                    minesweeperInput.enabled = false;

                break;

            case MinesweeperState.Lost:

                statusText.text = "¡DERROTA! Has encontrado una mina.";

                if (minesweeperInput != null)
                    minesweeperInput.enabled = false;

                break;
        }
    }


    private void OnCountersChanged(int mines, int flags, int remaining)
    {
        if (mineCountText != null)
            mineCountText.text = $"Minas: {mines}";

        if (flagCountText != null)
            flagCountText.text = $"Banderas: {flags}";

        if (remainingFlagsText != null)
            remainingFlagsText.text = $"Restantes: {remaining}";
    }



    private void OnModeChanged(int selectedMode)
    {
        bool isBotMode = selectedMode == 1;

        if (difficultyPanel != null)
            difficultyPanel.SetActive(isBotMode);
    }

}


