using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class MazeExplorerAgent : Agent
{
    [SerializeField] float _moveSpeed;
    [SerializeField] float _rotationSpeed;

    bool _hasKey;

    float _cumulativeReward;
    int _currentEpisode;
    Rigidbody _rb;

    MazeGenerator _mazeGenerator;
    public override void Initialize()
    {
        _mazeGenerator = transform.parent.GetComponent<MazeGenerator>();
        _rb = GetComponent<Rigidbody>();
    }
    public override void OnEpisodeBegin()
    {
        _hasKey = false;
        _currentEpisode++;
        _cumulativeReward = 0;
        _mazeGenerator.Generate();
        transform.localPosition = Vector3.up;
        transform.rotation = Quaternion.identity;
    }
    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition.x / 5f);
        sensor.AddObservation(transform.localPosition.z / 5f);

        if (_hasKey)
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
        }
        else
        {
            Vector3 keyPos = _mazeGenerator.GetKeyPos();
            sensor.AddObservation(keyPos.x / 5f);
            sensor.AddObservation(keyPos.z / 5f);
            sensor.AddObservation(1f);
        }

        Vector3 doorPos = _mazeGenerator.GetDoorPos();
        sensor.AddObservation(doorPos.x / 5f);
        sensor.AddObservation(doorPos.z / 5f);

        float angle = transform.localEulerAngles.y * Mathf.Deg2Rad;
        sensor.AddObservation(Mathf.Sin(angle));
        sensor.AddObservation(Mathf.Cos(angle));
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = 0;
        if (Input.GetKey(KeyCode.W))
        {
            discreteActionsOut[0] = 1;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[0] = 2;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[0] = 3;
        }

    }
    public override void OnActionReceived(ActionBuffers actions)
    {
        MoveAgent(actions.DiscreteActions);

        AddReward(-2f / MaxStep);
        _cumulativeReward = GetCumulativeReward();

        _mazeGenerator.UpdateStatText(_currentEpisode, _cumulativeReward, StepCount);
    }
    void MoveAgent(ActionSegment<int> actions)
    {
        var act = actions[0];

        switch (act)
        {
            case 1:
                _rb.MovePosition(_rb.position + transform.forward * _moveSpeed * Time.deltaTime);
                break;
            case 2:
                Quaternion deltaRotation1 = Quaternion.Euler(0f, -_rotationSpeed * Time.deltaTime, 0f);
                _rb.MoveRotation(_rb.rotation * deltaRotation1);
                break;
            case 3:
                Quaternion deltaRotation2 = Quaternion.Euler(0f, _rotationSpeed * Time.deltaTime, 0f);
                _rb.MoveRotation(_rb.rotation * deltaRotation2);
                break;
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.01f);
        }
    }
    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.002f * Time.fixedDeltaTime);
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("Key"))
        {
            AddReward(1f);
            _cumulativeReward = GetCumulativeReward();

            _hasKey = true;
            _mazeGenerator.DestroyKey();
        }
        if (other.gameObject.CompareTag("Door") && _hasKey == true)
        {
            AddReward(2f);
            _cumulativeReward = GetCumulativeReward();
            _mazeGenerator.UpdateStatText(_currentEpisode, _cumulativeReward, StepCount);

            EndEpisode();
        }
    }
}
