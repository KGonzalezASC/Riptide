using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MainMenuEvents : MonoBehaviour
{
    //Reference to menu's UI document
    private UIDocument _document;
    //Reference to menu's button
    private Button _button;
    //List of all buttons
    private List<Button> _menuButtons;
    //On click sound
    //Ensure play on awake is turned off in editor
    [SerializeField]
    private AudioSource _audioSource;

    //Reference for gameplay ui and state management
    [SerializeField]
    private VisualTreeAsset gameplayUXML;
    private VisualElement gameplayElement;
    private bool isGameplayActive = false;

    private void Awake()
    {
        //Get referecne to the uiDocument
        _document = GetComponent<UIDocument>();
        //Use reference to UI element to get root visual element
        _button = _document.rootVisualElement.Q("btn-start") as Button;
        //Register a callback to the button
        _button.RegisterCallback<ClickEvent>(OnPlayGameClick);

        //Obtain references to all buttons
        _menuButtons = _document.rootVisualElement.Query<Button>().ToList();
        //Register a callback for each button
        foreach (Button btn in _menuButtons)
        {
            btn.RegisterCallback<ClickEvent>(OnCallbackButtonsClick);
        }

    }

    /// <summary>
    /// Used to unregister events when obj is disabled
    /// </summary>
    private void OnDisable()
    {
        _button.UnregisterCallback<ClickEvent>(OnPlayGameClick);

        foreach (Button btn in _menuButtons)
        {
            btn.UnregisterCallback<ClickEvent>(OnCallbackButtonsClick);
        }
    }

    /// <summary>
    /// Actions that occur when the start button is clicked
    /// </summary>
    /// <param name="evt">event obj</param>
    private void OnPlayGameClick(ClickEvent evt)
    {
        Debug.Log("Pressed the Start Button");

        if (!isGameplayActive)
        {
            StartGameplay();
        }
    }

    /// <summary>
    /// Transitions from displaying menu to displaying the game
    /// </summary>
    private void StartGameplay()
    {
        //check if there is already a gameplay instance
        if (isGameplayActive) { return; }

        //if not, start gameplay
        isGameplayActive = true;

        //hide menu UI
        _document.rootVisualElement.style.display = DisplayStyle.None;

        //load and display gameplay UI
        VisualElement rootElement = GetComponent<UIDocument>().rootVisualElement;
        //get gameplay UXML doc
        gameplayElement = gameplayUXML.CloneTree();
        //add it to the root element
        rootElement.Add(gameplayElement);
        //display the gameplay UI
        gameplayElement.style.display = DisplayStyle.Flex;

        //pause gameplay while changes are occuring
        Time.timeScale = 0;
    }

    /// <summary>
    /// Transitions from displaying game UI to menu UI
    /// </summary>
    private void ResumeGameplay()
    {
        if (!isGameplayActive) { return; }

        //Hide gameplay UI
        gameplayElement.style.display = DisplayStyle.None;
        //show menu UI
        _document.rootVisualElement.style.display = DisplayStyle.None;
        isGameplayActive = false;

        //resume game logic
        Time.timeScale = 1;
    }

    /// <summary>
    /// Used to assign an event to all buttons on the menu
    /// </summary>
    /// <param name="evt">event obj</param>
    private void OnCallbackButtonsClick(ClickEvent evt)
    {
        //play sound on click
        //_audioSource.Play();
    }
}