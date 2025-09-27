using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum SettingType
{
    MapSize,
    CellSize,
    AgentMoveSpeed,
    AgentRotationSpeed,
    AgentMaxSteps
}


[System.Serializable]
public class SliderSetting
{
    public int minValue;
    public int maxValue;
    public int defaultValue;
    public TextMeshProUGUI valueText;
    public Slider valueSlider;
    public SettingType settingType;
}
public class ControlsManager : MonoBehaviour
{
    [SerializeField] SliderSetting[] settings;

    [Header("Toggles")]
    [SerializeField] Toggle autoUpdateToggle;
    [SerializeField] Toggle heuristicToggle;
    bool _autoUpdate = false;

    [Header("References")]
    [SerializeField] MazeGenerator mazeGenerator;
    [SerializeField] MazeExplorerAgent agent;

    private void Start()
    {
        for (int i = 0; i < settings.Length; i++)
        {
            var s = settings[i];
            s.valueSlider.minValue = s.minValue;
            s.valueSlider.maxValue = s.maxValue;
            s.valueSlider.value = s.defaultValue;
            s.valueText.text = $"{s.valueSlider.value}/{s.maxValue}";

            ApplySetting(s.settingType, (int)s.valueSlider.value);

            int index = i;
            s.valueSlider.onValueChanged.AddListener((value) =>
            {
                int finalValue = (int)value;

                if (s.settingType == SettingType.MapSize)
                {
                    finalValue = Mathf.Max(s.minValue, ((int)value / 2) * 2);
                }

                s.valueSlider.value = finalValue;
                s.valueText.text = $"{finalValue}/{s.maxValue}";

                ApplySetting(s.settingType, finalValue);

                if (_autoUpdate)
                {
                    RestartRun();
                }
            });
        }

        _autoUpdate = autoUpdateToggle.isOn;
        autoUpdateToggle.onValueChanged.AddListener((v) =>
        {
            _autoUpdate = v;
        });

        agent.ChangeBehaviourType(heuristicToggle.isOn);
        heuristicToggle.onValueChanged.AddListener((v) =>
        {
            agent.ChangeBehaviourType(v);
        });
    }

    void ApplySetting(SettingType settingType, int value)
    {
        if (settingType == SettingType.MapSize)
        {
            mazeGenerator.ApplyMapSize(value);
        }
        if(settingType == SettingType.CellSize)
        {
            mazeGenerator.ApplyCellSize(value);
        }
        if(settingType == SettingType.AgentMoveSpeed)
        {
            agent.ApplyMoveSpeed(value);
        }
        if(settingType == SettingType.AgentRotationSpeed)
        {
            agent.ApplyRotationSpeed(value);
        }
        if(settingType == SettingType.AgentMaxSteps)
        {
            agent.MaxStep = value;
        }
    }


    public void RestartRun()
    {
        agent.EndEpisode();
    }
}
