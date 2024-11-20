using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class DisplayComboText : MonoBehaviour
{
    private UIDocument _document;
    private VisualElement combo_container;
    private ProgressBar progressBar;

    //array of words that could be displayed
    string[] words = new string[] { "GNARLY", "SWAG-O-LICIOUS", "TUBULAR", "KABOOM", "FINCREDIBLE", "FLOP SHUV-IT", "FINSANE", "FISH OUT OF WATER" };

    /// <summary>
    /// Changes the text displayed in the combo UI container
    /// </summary>
    public void ChangeText()
    {
        //if in game state
        if (GameManager.instance.topState.GetName() == "Game")
        {
            combo_container = _document.rootVisualElement.Q<VisualElement>("container-combo"); //we need to this because the document changes when we switch states
            Label newLabel = GenerateComboLabel();
            newLabel.AddToClassList("label-combo");
            combo_container.Add(newLabel);
            AnimateLabel(newLabel);
        }
    }


    /// <summary>
    /// Clears the contents of the combo text's label
    /// </summary>
    public void ClearText()
    {
        combo_container = _document.rootVisualElement.Q<VisualElement>("container-combo"); //we need to this because the document changes when we switch states
        //empty label
        combo_container.Clear();

    }
    public void ClearBar()
    {
        progressBar = _document.rootVisualElement.Q<ProgressBar>("label-powerup");

        if (progressBar.style.visibility.Equals(Visibility.Visible))
        {
            progressBar.style.visibility = Visibility.Hidden;
        }
    }

    /// <summary>
    /// Genereates a new text phrase to be displayed in the user
    /// </summary>
    /// <returns>random text phrase to display</returns>
    Label GenerateComboLabel()
    {
        //random is max exclusive
        //get text to add
        int index = Random.Range(0, words.Length);
        return new Label { text = words[index] };
    }

    public void Awake()
    {
        _document = GetComponent<UIDocument>();
    }

    //using ref to pass by reference so we get the direct value of the timer, not a copy
    public void powerUpTimerLength(ref float powerUpTimer)
    {
        progressBar = _document.rootVisualElement.Q<ProgressBar>("label-powerup");
        if (GameManager.instance.topState.GetName() == "Game")
        {
            if (!progressBar.style.visibility.Equals(Visibility.Visible) && powerUpTimer > 19f) //you have to do manually 1 less than time because a tick has already passed
            {
                progressBar.style.visibility = Visibility.Visible;
            }
            else if (powerUpTimer <= 0)
            {
                progressBar.style.visibility = Visibility.Hidden; // Hide the bar when the timer ends
            }
            progressBar.value = powerUpTimer;
        }
    }

    /// <summary>
    /// Animates the combo text
    /// </summary>
    /// <param name="label">label to animate</param>
    private void AnimateLabel(Label label)
    {
        float duration = 2f; //total animation time in seconds
        float elapsedTime = 0f; //time since animation started

        label.schedule.Execute(() =>
        {
            //increment time
            elapsedTime += Time.deltaTime;
            //ensure label doesnt go past its finished angle
            float progress = Mathf.Clamp01(elapsedTime / duration);

            //update position (move in a sine wave path)
            float x = Mathf.Sin(progress * Mathf.PI * 2) * 50; //horizontal movement
            float y = Mathf.Cos(progress * Mathf.PI * 2) * 20; //vertical movement

            //apply translation
            label.style.translate = new StyleTranslate(new Translate(new Length(x, LengthUnit.Pixel), new Length(y, LengthUnit.Pixel), 0));

            //apply rotation
            //label.style.rotate = new StyleRotate(new Rotate(new Angle(progress * 360f, AngleUnit.Degree)));

            //apply upscaling
            label.style.scale = new StyleScale(new Vector2(x / 20, 1));
            //is animation finished
            if (progress >= 1f)
            {
                combo_container.Remove(label);
            }
        }).Until(() => elapsedTime >= duration);
    }

}
