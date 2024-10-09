using System; 
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

//todo move to be part of the load state
public class MainMenuEvents : MonoBehaviour
{
    private UIDocument _document;
    private Button _button;
    private List<Button> _menuButtons;
    // On click sound (ensure play on awake is turned off in the editor)
    [SerializeField] private AudioSource _audioSource;

    // Reference to gameplay UI and state management
    [SerializeField] private VisualTreeAsset gameplayUXML;
    [SerializeField] private VisualTreeAsset loseUXML;
    public event Action OnPlayButtonClicked;
    private VisualTreeAsset menuUXML; // Optional: Keep a reference to the original menu UXML
    private bool isGameplayActive = false; //honestly not sure if this safeguard will be needed in future

    private void BindStartUI() //when visual tree asset changes it needs to be binded as well.
    {
        _button = _document.rootVisualElement.Q<Button>("btn-start");
        _button.RegisterCallback<ClickEvent>(OnPlayGameClick);
        _menuButtons = _document.rootVisualElement.Query<Button>().ToList();
        foreach (Button btn in _menuButtons)
        {
            btn.RegisterCallback<ClickEvent>(OnCallbackButtonsClick);
        }
    }


    private void BindLoseUI()
    {
        //insert binding buttons for lose screen
    }




    public void OnRestart()
    {
        isGameplayActive = false;
        //set visual tree asset to nothing
        _document.visualTreeAsset = loseUXML;
        BindLoseUI();
    }


    private void Awake()
    {
        _document = GetComponent<UIDocument>();
        BindStartUI();
        menuUXML = _document.visualTreeAsset;
    }

    private void OnDisable()
    {
        _button.UnregisterCallback<ClickEvent>(OnPlayGameClick);
        foreach (Button btn in _menuButtons)
        {
            btn.UnregisterCallback<ClickEvent>(OnCallbackButtonsClick);
        }
    }

    /// Actions that occur when the start button is clicked
    /// <param name="evt">event object</param>
    private void OnPlayGameClick(ClickEvent evt)
    {
        if (!isGameplayActive)
        {
            StartGameplay();
            OnPlayButtonClicked?.Invoke();
        }
    }

    /// Transitions from displaying the menu to displaying the gameplay UI
    public void StartGameplay()
    {
        if (isGameplayActive) { return; }
        // Mark gameplay as active
        isGameplayActive = true;
        _document.visualTreeAsset = gameplayUXML;
    }

    //we dont have pause functionality yet
    private void ResumeGameplay()
    {
        if (!isGameplayActive) { return; }
        _document.visualTreeAsset = menuUXML;  
        // Resume game logic
        isGameplayActive = false;
        Time.timeScale = 1;
        Debug.Log("Switched back to Menu UI");
    }

    /// Used to assign an event to all buttons on the menu
    /// <param name="evt">event object</param>
    private void OnCallbackButtonsClick(ClickEvent evt)
    {
        // Play sound on click
        //_audioSource.Play();
    }
}
