using System;
using System.Collections.Generic;
using UnityEngine;

namespace TestPhysics
{
    #region ControllerModes

    public enum FlightMode
    {
        Acro,
        Angle,
        Horizon,
        Stable,
        Track,
        Synthetic
    }

    public enum ThrottleScheme
    {
        Inverted,
        Direct
    }

    public enum FlightStatus
    {
        Armed,
        Disarmed
    }

    public enum RotorType
    {
        QuadroBaseM4,
        HexaBaseM6,
        OctoBaseM8
    }

    public enum AirCraftType
    {
        Multirotor,
        AirPlane
    }

    public enum AirPlaneScheme
    {
        Standart
    }

    #endregion

    #region MultirotorDrone

    [Serializable]
    public class PidComponent
    {
        public Vector3 Coefficients = Vector3.one;
        public Vector3 Limits = Vector3.one;
    }

    [Serializable]
    public class PidController3
    {
        [Range(0f, 1f)]
        public float FilterCoefficient = 0.0f;

        // Controller coefficients and limits
        public PidComponent Proportional;
        public PidComponent Integral;
        public PidComponent Differential;

        // Output of PID
        [HideInInspector]
        public Vector3 OutputSignal = Vector3.zero;

        // Input and Output values (previous status)
        private Vector3 _currentInput = Vector3.zero;
        private Vector3 _lastInput = Vector3.zero;
        private Vector3 _delta = Vector3.zero;
        private Vector3 _integral = Vector3.zero;
        private Vector3 _proportional = Vector3.zero;
        private Vector3 _differential = Vector3.zero;

        // Update output values
        public void Compute(Vector3 input)
        {
            // Compute logarithmic coefficient
            var tempCoefficient = 1 - Mathf.Pow(100f, -FilterCoefficient);

            // Filtrate input error
            _currentInput = tempCoefficient * _currentInput + (1f - tempCoefficient) * input;
            _delta = tempCoefficient * _delta + (1f - tempCoefficient) * (input - _lastInput);

            // Compute output signal
            for (var i = 0; i < 3; i++)
            {
                // Update error, derivative, integral sum
                _proportional[i] = Normalize(Proportional.Coefficients[i] * _currentInput[i], Proportional.Limits[i]);
                _integral[i] = Normalize(_integral[i] + Time.fixedDeltaTime * Integral.Coefficients[i] * _currentInput[i], Integral.Limits[i]);
                _differential[i] = Normalize(Differential.Coefficients[i] * _delta[i] / Time.fixedDeltaTime, Differential.Limits[i]);
            }

            // Compute control signal
            OutputSignal = _proportional + _differential + _integral;

            // Update last values
            _lastInput = input;
        }

        // Normalize input argument with limited linear function
        private float Normalize(float x, float a) => Mathf.Abs(x) > a ? Mathf.Sign(x) * a : x;
    }

    [Serializable]
    public class AcroConverter
    {
        // Betaflight angular rates
        public Vector3 RateRC = Vector3.one * 1f;
        public Vector3 RateS = Vector3.one * 0.7f;
        public Vector3 Expo = Vector3.one * 0f;

        // Betaflight throttle rates
        [Range(0f, 1f)]
        public float MidTH = 0f;
        [Range(0f, 1f)]
        public float ExpoTH = 0f;

        [HideInInspector]
        public Vector4 ControlSignal = Vector4.zero;

        // Throttle curve
        public float ThrottleCurve(float x)
        {
            // Prevent division by zero
            var eps = 1e-4f;

            // Two conditions
            return x > MidTH ? ExpoTH / (1f + eps - MidTH) * (x - MidTH) * (x - 1f) + x : -ExpoTH / (eps + MidTH) * (x - MidTH) * x + x;
        }

        // Update control signal
        public void UpdateControlSignal(Vector4 input)
        {
            // Refresh angular signal
            for (var i = 0; i < 3; i++)
            {
                var x = input[i];
                var p = 1f / (1f - Mathf.Abs(x) * RateS[i]);
                var r = 200f * RateRC[i] * (Expo[i] * Mathf.Pow(x, 4f) + x * (1f - Expo[i]));

                ControlSignal[i] = r * p;
            }
            ControlSignal.w = ThrottleCurve((input.w + 1f) / 2f);
        }
    }

    [Serializable]
    public class AngleConverter
    {
        // Betaflight angular rates
        public Vector3 RateRC = Vector3.one * 0.08f;
        public Vector3 RateS = Vector3.one * 0.7f;
        public Vector3 Expo = Vector3.one * 0f;

        // Betaflight throttle rates
        [Range(0f, 1f)]
        public float MidTH = 0f;
        [Range(0f, 1f)]
        public float ExpoTH = 0f;

        [HideInInspector]
        public Vector4 ControlSignal = Vector4.zero;

        // Throttle curve
        public float ThrottleCurve(float x)
        {
            // Prevent division by zero
            var eps = 1e-4f;

            // Two conditions
            return x > MidTH ? ExpoTH / (1f + eps - MidTH) * (x - MidTH) * (x - 1f) + x : -ExpoTH / (eps + MidTH) * (x - MidTH) * x + x;
        }

        // Update control signal
        public void UpdateControlSignal(Vector4 input)
        {
            // Refresh angular signal
            for (var i = 0; i < 3; i++)
            {
                var x = input[i];
                var p = 1f / (1f - Mathf.Abs(x) * RateS[i]);
                var r = 200f * RateRC[i] * (Expo[i] * Mathf.Pow(x, 4f) + x * (1f - Expo[i]));

                ControlSignal[i] = r * p;
            }
            ControlSignal.w = ThrottleCurve((input.w + 1f) / 2f);
        }
    }

    [Serializable]
    public class StableConverter
    {
        // Betaflight angular rates
        public Vector4 RateRC = Vector4.one * 0.08f;
        public Vector4 RateS = Vector4.one * 0.7f;
        public Vector4 Expo = Vector4.one * 0f;

        [HideInInspector]
        public Vector4 ControlSignal = Vector4.zero;

        // Update control signal
        public void UpdateControlSignal(Vector4 input)
        {
            // Refresh angular signal
            for (var i = 0; i < 4; i++)
            {
                var x = input[i];
                var p = 1f / (1f - Mathf.Abs(x) * RateS[i]);
                var r = 200f * RateRC[i] * (Expo[i] * Mathf.Pow(x, 4f) + x * (1f - Expo[i]));

                ControlSignal[i] = r * p;
            }
        }
    }

    [Serializable]
    public class AcroManager
    {
        // Main parameters
        public AcroConverter Converter;
        public PidController3 Pid;

        [HideInInspector]
        public Vector4 OutputSignal = Vector4.zero;

        // Update control signal
        public void Compute(Vector4 input, Rigidbody refBody)
        {
            // Use control curves
            Converter.UpdateControlSignal(input);

            // Compute target and current angular velocity
            Vector3 targetVelocity = Mathf.Deg2Rad * Converter.ControlSignal;
            Vector3 currentVelocity = refBody.transform.InverseTransformVector(refBody.angularVelocity);

            // Use PID controller
            Pid.Compute(targetVelocity - currentVelocity);
            OutputSignal = Pid.OutputSignal;
            OutputSignal.w = Converter.ControlSignal.w;
        }
    }

    [Serializable]
    public class AngleManager
    {
        // Main parameters
        public AngleConverter Converter;
        public PidController3 Pid;

        [HideInInspector]
        public Vector4 OutputSignal = Vector4.zero;

        // Update control signal
        public void Compute(Vector4 input, Rigidbody refBody)
        {
            // Use control curves
            Converter.UpdateControlSignal(input);

            // Compute target and current angular values
            Vector3 targetValues = Mathf.Deg2Rad * Converter.ControlSignal;
            Vector3 currentValues = Vector3.zero;
            Vector3 up = refBody.transform.InverseTransformVector(Vector3.up);

            // Compute current values
            currentValues.x = -Mathf.Atan2(up.z, up.y);
            currentValues.y = refBody.transform.InverseTransformVector(refBody.angularVelocity).y;
            currentValues.z = Mathf.Atan2(up.x, up.y);

            // Use PID controller
            Pid.Compute(targetValues - currentValues);
            OutputSignal = Pid.OutputSignal;
            OutputSignal.w = Converter.ControlSignal.w;
        }
    }

    [Serializable]
    public class HorizonManager
    {
        // Participation factor
        public float ProportionFactor = 0.1f;

        // Main mode parameters
        public AcroManager AcroComponent;
        public AngleManager AngleComponent;

        [HideInInspector]
        public Vector4 OutputSignal = Vector4.zero;

        // Update control signal
        public void Compute(Vector4 input, Rigidbody refBody)
        {
            // Update component's control signals
            AcroComponent.Compute(input, refBody);
            AngleComponent.Compute(input, refBody);

            // Make combination of signals
            //var alpha = Mathf.Pow(joySignal.magnitude / Mathf.Sqrt(3), 0.1f);
            var alpha = Mathf.Pow(Magnitude(input), ProportionFactor);

            for (var i = 0; i < 4; ++i)
            {
                OutputSignal[i] = alpha * AcroComponent.OutputSignal[i] + (1f - alpha) * AngleComponent.OutputSignal[i];
            }
        }

        // Magnitude Max(|...|)
        public float Magnitude(Vector4 v) => Mathf.Max(Mathf.Max(Mathf.Abs(v.x), Mathf.Abs(v.y)), Mathf.Abs(v.z));
    }

    [Serializable]
    public class StableManager
    {
        // Main parameters
        public StableConverter Converter;
        public PidController3 Pid;

        // Angle manager for ability to control drone
        public AngleManager AngleComponent;

        [HideInInspector]
        public Vector4 OutputSignal = Vector4.zero;

        // Update control signal
        public void Compute(Vector4 input, Rigidbody refBody)
        {
            // Use control curves
            Converter.UpdateControlSignal(input);

            // Find target velocity
            Vector3 xOne = refBody.transform.TransformVector(Vector3.right);
            Vector3 zOne = refBody.transform.TransformVector(Vector3.forward);
            Vector3 currentValues = refBody.velocity;

            xOne.y = 0f;
            zOne.y = 0f;

            currentValues.x = Vector3.Dot(refBody.velocity, xOne.normalized);
            currentValues.z = Vector3.Dot(refBody.velocity, zOne.normalized);

            // Use PID controller (PID limits need to be in interval [-1, 1])
            var targetValues = new Vector3(-Converter.ControlSignal.z, Converter.ControlSignal.w, Converter.ControlSignal.x);
            Vector3 error = targetValues - currentValues;
            Pid.Compute(error);

            // Use angle mode to move drone
            Vector4 angleInput = Normalize(new Vector4(Pid.OutputSignal.z, input.y, -Pid.OutputSignal.x, Pid.OutputSignal.y));

            angleInput.x *= AngleCoefficient(Mathf.Abs(error.z), Mathf.Abs(error.x) + Mathf.Abs(error.z));
            angleInput.z *= AngleCoefficient(Mathf.Abs(error.x), Mathf.Abs(error.x) + Mathf.Abs(error.z));

            AngleComponent.Compute(angleInput, refBody);
            OutputSignal = AngleComponent.OutputSignal;
        }

        public float Normalize(float x) => x < -1f ? -1f : x > 1f ? 1f : x;
        public Vector4 Normalize(Vector4 x)
        {
            Vector4 temp = x;
            for (var i = 0; i < 4; ++i)
            {
                temp[i] = Normalize(temp[i]);
            }
            return temp;
        }
        public float AngleCoefficient(float x, float y) => y < 0.001f ? 0.5f : x / y;
    }

    [Serializable]
    public class TrackManager
    {
        // Main parameters
        public Vector3 Position;
        public PidController3 VelocityPid;
        public PidController3 PositionPid;

        // Angle manager for ability to control drone
        public AngleManager AngleComponent;

        [HideInInspector]
        public Vector4 OutputSignal = Vector4.zero;

        // Update control signal
        public void Compute(Vector4 input, Rigidbody refBody)
        {
            // Use pid(position error) to calculate drone velocities
            PositionPid.Compute(Position - refBody.position);

            // Use pid(velocity error) to calculate drone angles in angle flight mode
            VelocityPid.Compute(refBody.transform.InverseTransformVector(PositionPid.OutputSignal - refBody.velocity));

            // Use angle mode to move drone
            AngleComponent.Compute(new Vector4(VelocityPid.OutputSignal.z, input.y, -VelocityPid.OutputSignal.x, Normalize(VelocityPid.OutputSignal.y)), refBody);
            OutputSignal = AngleComponent.OutputSignal;
        }

        public float Normalize(float x)
        {
            x = 2f * x - 1f;
            return x < -1f ? -1f : x > 1f ? 1f : x;
        }
    }

    [Serializable]
    public class SyntheticManager
    {
        // Main parameters
        public Vector4 ControlSignal = Vector4.zero;
        public PidController3 Pid;

        [HideInInspector]
        public Vector4 OutputSignal = Vector4.zero;

        // Update control signal
        public void Compute(Rigidbody refBody)
        {
            // Compute target and current angular velocity
            Vector3 targetVelocity = Mathf.Deg2Rad * ControlSignal;
            Vector3 currentVelocity = refBody.transform.InverseTransformVector(refBody.angularVelocity);

            // Use PID controller
            Pid.Compute(targetVelocity - currentVelocity);
            OutputSignal = Pid.OutputSignal;
            OutputSignal.w = ControlSignal.w;
        }
    }

    [Serializable]
    public class MultirotorManager
    {
        // Engines of the drone
        public List<SEMotor> Engines;

        // Choose different modes
        public RotorType MotorBase = RotorType.QuadroBaseM4;
        public FlightMode ControlMode = FlightMode.Acro;
        public ThrottleScheme ThrustScheme = ThrottleScheme.Direct;

        // Main flight modes settings
        public AcroManager AcroComponent;
        public AngleManager AngleComponent;
        public HorizonManager HorizonComponent;
        public StableManager StableComponent;
        public TrackManager TrackComponent;
        public SyntheticManager SyntheticComponent;

        // Compute throttle distribution with throttle scheme
        public void ComputeThrottleM4(Vector4 input)
        {
            // Prime throttle distribution
            Engines[0].Throttle = -input.x + input.y + input.z + input.w;
            Engines[1].Throttle = input.x - input.y + input.z + input.w;
            Engines[2].Throttle = input.x + input.y - input.z + input.w;
            Engines[3].Throttle = -input.x - input.y - input.z + input.w;

            // Normalize throttles with special rules
            Normalize();
        }
        public void ComputeThrottleM6(Vector4 input)
        {
            // Prime throttle distribution
            Engines[0].Throttle = -input.x + input.y + input.z + input.w;
            Engines[1].Throttle = input.x - input.y + input.z + input.w;
            Engines[2].Throttle = input.x + input.y + input.w;
            Engines[3].Throttle = input.x - input.y - input.z + input.w;
            Engines[4].Throttle = -input.x + input.y - input.z + input.w;
            Engines[5].Throttle = -input.x - input.y + input.w;

            // Normalize throttles with special rules
            Normalize();
        }
        public void ComputeThrottleM8(Vector4 input)
        {
            // Prime throttle distribution
            Engines[0].Throttle = -input.x + input.y + input.z + input.w;
            Engines[1].Throttle = input.x - input.y + input.z + input.w;
            Engines[2].Throttle = input.x + input.y + input.z + input.w;
            Engines[3].Throttle = input.x - input.y - input.z + input.w;
            Engines[4].Throttle = input.x + input.y - input.z + input.w;
            Engines[5].Throttle = -input.x - input.y - input.z + input.w;
            Engines[6].Throttle = -input.x + input.y - input.z + input.w;
            Engines[7].Throttle = -input.x - input.y + input.z + input.w;

            // Normalize throttles with special rules
            Normalize();
        }

        // Compute throttle automatically for M4, M6, M8
        public void ComputeThrottle(Vector4 input)
        {
            switch (MotorBase)
            {
                case RotorType.QuadroBaseM4:
                    ComputeThrottleM4(input);
                    break;
                case RotorType.HexaBaseM6:
                    ComputeThrottleM6(input);
                    break;
                case RotorType.OctoBaseM8:
                    ComputeThrottleM8(input);
                    break;
                default:
                    ComputeThrottleM4(input);
                    break;
            }
        }

        // Normalize engines throttle
        public void Normalize()
        {
            for (var i = 0; i < Engines.Count; ++i)
            {
                // Check if throttle is out of range
                if (Mathf.Abs(Engines[i].Throttle) > 1f)
                {
                    Engines[i].Throttle = Mathf.Sign(Engines[i].Throttle);
                }

                // Use direct or inverse scheme
                if (ThrustScheme == ThrottleScheme.Direct && Engines[i].Throttle < 0f)
                {
                    Engines[i].Throttle = 0f;
                }

                // Normalize
                Engines[i].Throttle = Mathf.Sign(Engines[i].Throttle) * Mathf.Sqrt(Mathf.Abs(Engines[i].Throttle));
            }
        }

        // Clear engines throttle
        public void Clear()
        {
            for (var i = 0; i < Engines.Count; ++i)
            {
                Engines[i].Throttle = 0f;
            }
        }

        // Update engine throttles
        public void Update(Vector4 inputSignal, Rigidbody refBody, FlightStatus status)
        {
            if (status == FlightStatus.Armed)
            {
                switch (ControlMode)
                {
                    case FlightMode.Acro:
                        AcroComponent.Compute(inputSignal, refBody);
                        ComputeThrottle(AcroComponent.OutputSignal);
                        break;
                    case FlightMode.Angle:
                        AngleComponent.Compute(inputSignal, refBody);
                        ComputeThrottle(AngleComponent.OutputSignal);
                        break;
                    case FlightMode.Horizon:
                        HorizonComponent.Compute(inputSignal, refBody);
                        ComputeThrottle(HorizonComponent.OutputSignal);
                        break;
                    case FlightMode.Stable:
                        StableComponent.Compute(inputSignal, refBody);
                        ComputeThrottle(StableComponent.OutputSignal);
                        break;
                    case FlightMode.Track:
                        TrackComponent.Compute(inputSignal, refBody);
                        ComputeThrottle(TrackComponent.OutputSignal);
                        break;
                    case FlightMode.Synthetic:
                        SyntheticComponent.Compute(refBody);
                        ComputeThrottle(SyntheticComponent.OutputSignal);
                        break;
                    default:
                        AcroComponent.Compute(inputSignal, refBody);
                        ComputeThrottle(AcroComponent.OutputSignal);
                        break;
                }
            }
            else
            {
                Clear();
            }
        }
    }

    #endregion

    #region AirPlaneDrone

    [Serializable]
    public class AirPlaneConverter
    {
        // Wing flap rates
        public Vector3 ZeroRate = Vector3.one * 0f;
        public Vector3 ExpoRate = Vector3.one * 0f;

        // Throttle rates
        [Range(0f, 1f)]
        public float MidTH = 0f;
        [Range(0f, 1f)]
        public float ExpoTH = 0f;

        [HideInInspector]
        public Vector4 OutputSignal = Vector4.zero;

        // Throttle curve
        public float ThrottleCurve(float x)
        {
            // Prevent division by zero
            var eps = 1e-4f;

            // Two conditions
            return x > MidTH ? ExpoTH / (1f + eps - MidTH) * (x - MidTH) * (x - 1f) + x : -ExpoTH / (eps + MidTH) * (x - MidTH) * x + x;
        }

        // Flap angle curve
        public float AngleCurve(float x, float rate, float expo) => (1f - rate) * x + rate * ((1f - expo) * Mathf.Pow(x, 3f) + expo * Mathf.Pow(x, 9f));

        // Update control signal
        public void Update(Vector4 input)
        {
            for (var i = 0; i < 3; i++)
            {
                OutputSignal[i] = AngleCurve(input[i], ZeroRate[i], ExpoRate[i]);
            }
            OutputSignal.w = ThrottleCurve((input.w + 1f) / 2f);
        }
    }

    [Serializable]
    public class StandartManager
    {
        // Two main wings
        public Wing LeftMainWing;
        public Wing RightMainWing;

        // Tail fin
        public Transform LeftTailWing;
        public Transform RightTailWing;
        public Transform UpperTailWing;

        // Z rotation limits
        public float MaxTailAngle = 45f;

        // Apply control signals for flaps and engines
        public void ApplyControlSignal(Vector4 input)
        {
            // Rotate wing flap
            LeftMainWing.FlapParameters.RelativeAngle = Normalize(-input.x + input.z);
            RightMainWing.FlapParameters.RelativeAngle = Normalize(input.x + input.z);

            // Rotate wing itself
            LeftTailWing.localEulerAngles = new Vector3(0f, 0f, MaxTailAngle * Normalize(input.x + input.z));
            RightTailWing.localEulerAngles = new Vector3(0f, 0f, MaxTailAngle * Normalize(-input.x + input.z));
            UpperTailWing.localEulerAngles = new Vector3(0f, 0f, MaxTailAngle * Normalize(input.y));
        }

        // Clear angles and throttles
        public void ClearControlSignal()
        {
            // Rotate wing flap
            LeftMainWing.FlapParameters.RelativeAngle = 0f;
            RightMainWing.FlapParameters.RelativeAngle = 0f;

            // Rotate wing itself
            LeftTailWing.localEulerAngles = Vector3.zero;
            RightTailWing.localEulerAngles = Vector3.zero;
            UpperTailWing.localEulerAngles = Vector3.zero;
        }

        public float Normalize(float x)
        {
            if (Mathf.Abs(x) > 1f)
            {
                x = Mathf.Sign(x);
            }
            return x;
        }
    }

    [Serializable]
    public class AirPlaneManager
    {
        // Engines of the drone
        public List<Motor> Engines;

        // Choose different modes
        public AirPlaneScheme Scheme = AirPlaneScheme.Standart;

        // Control managers
        public AirPlaneConverter Converter;
        public StandartManager StandartComponent;

        // Apply control signals for engines
        public void ApplyControlSignal(Vector4 input)
        {
            // Update engines throttle
            for (var i = 0; i < Engines.Count; ++i)
            {
                // Update throttle
                Engines[i].Throttle = input.w;

                // Check if throttle is out of range
                if (Mathf.Abs(Engines[i].Throttle) > 1f)
                {
                    Engines[i].Throttle = Mathf.Sign(Engines[i].Throttle);
                }

                // Normalize throttles
                Engines[i].Throttle = Mathf.Sign(Engines[i].Throttle) * Mathf.Sqrt(Mathf.Abs(Engines[i].Throttle));
            }

            // Update wing angles
            switch (Scheme)
            {
                case AirPlaneScheme.Standart:
                    StandartComponent.ApplyControlSignal(input);
                    break;
                default:
                    StandartComponent.ApplyControlSignal(input);
                    break;
            }
        }

        // Clear throttles
        public void Clear()
        {
            // Clear throttles
            for (var i = 0; i < Engines.Count; ++i)
            {
                // Update throttle
                Engines[i].Throttle = 0f;
            }

            // Clear angles
            switch (Scheme)
            {
                case AirPlaneScheme.Standart:
                    StandartComponent.ClearControlSignal();
                    break;
                default:
                    StandartComponent.ClearControlSignal();
                    break;
            }
        }

        public void Update(Vector4 inputSignal, FlightStatus status)
        {
            if (status == FlightStatus.Armed)
            {
                Converter.Update(inputSignal);
                ApplyControlSignal(Converter.OutputSignal);
            }
            else
            {
                Clear();
            }
        }
    }

    #endregion

    public class FlightController : PhysicObject
    {
        [Space]
        [Range(0f, 1f)]
        public float RelativeFrequency = 1f;
        public FlightStatus Status = FlightStatus.Armed;
        public AirCraftType DroneType = AirCraftType.Multirotor;

        public MultirotorManager MultirotorController;
        public AirPlaneManager AirPlaneController;

        [HideInInspector]
        public Vector4 InputSignal = Vector4.zero;

        public bool EventStarted(float p) => UnityEngine.Random.value <= p;

        protected virtual void FixedUpdate()
        {
            if (EventStarted(RelativeFrequency))
            {
                switch (DroneType)
                {
                    case AirCraftType.Multirotor:
                        MultirotorController.Update(InputSignal, ConnectedBody, Status);
                        AirPlaneController.Clear();
                        break;
                    case AirCraftType.AirPlane:
                        AirPlaneController.Update(InputSignal, Status);
                        MultirotorController.Clear();
                        break;
                    default:
                        MultirotorController.Update(InputSignal, ConnectedBody, Status);
                        AirPlaneController.Clear();
                        break;
                }
            }

        }
        protected override void OnDisable()
        {
            base.OnDisable();
            switch (DroneType)
            {
                case AirCraftType.Multirotor:
                    MultirotorController.Clear();
                    break;
                case AirCraftType.AirPlane:
                    AirPlaneController.Clear();
                    break;
                default:
                    MultirotorController.Clear();
                    break;
            }
        }
    }
}
