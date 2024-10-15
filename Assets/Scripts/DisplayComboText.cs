using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class DisplayComboText : MonoBehaviour
{
    private UIDocument _document;
    private Label _label;

    //array of words that could be displayed
    string[] words = new string[] { "GNARLY", "SWAG-O-LICIOUS", "TUBULAR", "KABOOM", "FINCREDIBLE", "FLOP SHUV-IT", "FINSANE", "FISH OUT OF WATER" };


    /// <summary>
    /// Changes the text displayed in the combo UI container
    /// </summary>
    public void ChangeText()
    {
        _label = _document.rootVisualElement.Q<Label>("label-combo");
        _label.text = GenerateText();
    }

    /// <summary>
    /// Clears the contents of the combo text's label
    /// </summary>
    public void ClearText()
    {
        //set text to empty string
        _label.text = string.Empty;
    }

    /// <summary>
    /// Genereates a new text phrase to be displayed in the user
    /// </summary>
    /// <returns>random text phrase to display</returns>
    string GenerateText()
    {
        //random is max exclusive
        int index = Random.Range(0, words.Length);
        //make sure new words display each time
        while (words[index] == _label.text)
        {
            //redo 
            index = Random.Range(0, words.Length);
        }
        return words[index];
    }

    public void Awake()
    {
        _document = GetComponent<UIDocument>();
        _label = _document.rootVisualElement.Q<Label>("label-combo");
        ChangeText();
    }
}
