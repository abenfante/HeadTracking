using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Reflection;
using UnityEngine.Events;


public class SettingsSliders : MonoBehaviour
{

    //private string[] sliderLabels;
    //private Vector2[] sliderLimits;

    [SerializeField]
    VisualTreeAsset sliderRowAsset;

    private VisualElement rootVisualElement;
    private VisualElement container;
    public List<UnityEvent<float>> SettingsUpdateEvents;

    private void OnEnable()
    {
        rootVisualElement = GetComponent<UIDocument>().rootVisualElement;
        container = rootVisualElement.Q<VisualElement>("SettingsContainer");
    }



    internal UnityEvent<float> CreateFloatSlider(float initValue, float minValue, float maxValue, string label)
    {
        var UpdateEvent = new UnityEvent<float>();

        // Add a slider to visual tree
        VisualElement sliderRow = sliderRowAsset.Instantiate();
        container.Add(sliderRow);

        // Get the slider and textfield inside them
        Slider slider = sliderRow.Q<Slider>();
        TextField textField = sliderRow.Q<TextField>();

        // Set textfield label
        textField.label = label;

        // Set slider limits
        slider.lowValue = minValue;
        slider.highValue = maxValue;

        // Set initial values of sliders
        slider.value = initValue;
        textField.value = initValue.ToString();

        // When the value of the slider changes update the textfield and invoke the personalized update event
        slider.RegisterValueChangedCallback((ChangeEvent<float> evt) =>
        {
            textField.SetValueWithoutNotify(evt.newValue.ToString());
            UpdateEvent.Invoke(evt.newValue);
        });

        // When textfield changes, value is validated, slider is updated, and personalized update event is called
        textField.RegisterValueChangedCallback((ChangeEvent<string> evt) =>
        {
            if (float.TryParse(evt.newValue, out float newFloat))
            {
                if (newFloat <= slider.highValue && newFloat >= slider.lowValue)
                {
                    slider.SetValueWithoutNotify(newFloat);
                    UpdateEvent.Invoke(newFloat);
                    return;
                }
            }

            //reset to default value if textfield is emptied
            if (evt.newValue == string.Empty)
            {
                slider.SetValueWithoutNotify(initValue);
                textField.SetValueWithoutNotify(initValue.ToString());
                UpdateEvent.Invoke(initValue);
                return;
            }

            //if validation fails and field is not empty, refuse the update
            textField.SetValueWithoutNotify(evt.previousValue);
        });

        // Return the update event so others can subscribe to it
        return UpdateEvent;
    }

}

