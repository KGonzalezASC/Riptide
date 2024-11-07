using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class DisplayComboText : MonoBehaviour
{
    private UIDocument _document;
    private Label combo_label;
    private ProgressBar progressBar;

    //array of words that could be displayed
    string[] words = new string[] { "GNARLY", "SWAG-O-LICIOUS", "TUBULAR", "KABOOM", "FINCREDIBLE", "FLOP SHUV-IT", "FINSANE", "FISH OUT OF WATER" };


    /// <summary>
    /// Changes the text displayed in the combo UI container
    /// </summary>
    public void ChangeText()
    {
        combo_label = _document.rootVisualElement.Q<Label>("label-combo"); //we need to this because the document changes when we switch states
        combo_label.text = GenerateText();
    }

    public void StartPowerSlider()
    {
        progressBar = _document.rootVisualElement.Q<ProgressBar>("label-powerup");
        // Check if the progress bar is currently visible
        if (!progressBar.style.visibility.Equals(Visibility.Visible))
        {
            progressBar.style.visibility = Visibility.Visible;
            StartCoroutine(DecrementProgressBar(13f)); // Pass 13 seconds as the duration
        }
    }

    private IEnumerator DecrementProgressBar(float duration)
    {
        progressBar.value = 100;
        float delay = 0.1f;
        float decrementAmount = 100f / (duration / delay);

        // Continue decrementing over the specified duration
        while (progressBar.value > 0)
        {
            progressBar.value -= decrementAmount;
            yield return new WaitForSeconds(delay);
        }

        // Hide the progress bar once it reaches 0
        progressBar.style.visibility = Visibility.Hidden;
        //stop coroutine
        StopCoroutine(DecrementProgressBar(duration));
    }




    /// <summary>
    /// Clears the contents of the combo text's label
    /// </summary>
    public void ClearText()
    {
        //set text to empty string
        combo_label.text = string.Empty;
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
        while (words[index] == combo_label.text)
        {
            //redo 
            index = Random.Range(0, words.Length);
        }
        return words[index];
    }

    public void Awake()
    {
        _document = GetComponent<UIDocument>();
    }
}
