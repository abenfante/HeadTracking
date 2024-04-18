using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System;
public class SettingsSliders : MonoBehaviour
{

    public string[] sliderNames;
    
    public Vector2[] sliderLimits;

    [SerializeField]
    VisualTreeAsset sliderRowAsset;

    private void OnEnable()
    {
        VisualElement rootVisualElement = GetComponent<UIDocument>().rootVisualElement;
        
        int index = 0;

        VisualElement container = rootVisualElement.Q<VisualElement>("SettingsContainer");

        foreach (var sliderName in sliderNames)
        {

            VisualElement sliderRow = sliderRowAsset.Instantiate();
            container.Add(sliderRow);

            Slider slider = sliderRow.Q<Slider>();
            TextField textField = sliderRow.Q<TextField>();

            // Set textfield label
            textField.label = sliderName;
            
            // Set slider limits
            slider.lowValue = sliderLimits[index].x;
            slider.highValue = sliderLimits[index].y;

            // Subscribe textfield to updates of slider
            slider.RegisterValueChangedCallback<float>( (ChangeEvent<float> evt) =>
            {
                textField.value = evt.newValue.ToString();
            });

            // Subscribe validation logic and slider to updates of textfield
            textField.RegisterValueChangedCallback<string>((ChangeEvent<string> evt) =>
            {
                float newFloat;
                if(float.TryParse(evt.newValue, out newFloat))
                {
                    if (newFloat <= slider.highValue && newFloat >= slider.lowValue)
                    {
                        slider.value = newFloat;
                        return;
                    }
                }
                
                if(evt.newValue == string.Empty)
                {
                    slider.value = slider.lowValue;
                    textField.value = slider.lowValue.ToString();
                    return;
                }

                textField.value = evt.previousValue;
            });


            index++;
        }
    }
}

