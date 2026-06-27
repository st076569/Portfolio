using System;
using UnityEngine;

namespace TestPhysics
{
    public enum PropellerOrientation
    {
        Positive,
        Negative
    }

    [Serializable]
    public class PropellerData
    {
        public float Pitch = 5.1f;    // [inch]
        public float Diameter = 7f;     // [inch]
        public int BladesNumber = 3;    // [-]
        
        [Range(0.15f, 0.6f)]
        public float ThrustAlpha = 0.17f;
        [Range(0.75f, 1.5f)]
        public float TorqueAlpha = 1f;
        [Range(1f, 4f)]
        public float ViscousAlpha = 1f;
    }

    [Serializable]
    public class PropellerDamageData
    {
        [Range(0f, 1f)]
        public float EccentricCoefficient = 1f;
        [Range(0.05f, 0.2f)]
        public float AngularVelocityCoefficient = 0.1f;
    }

    public class Propeller : AerodynamicObject
    {
        [Space]
        public PropellerOrientation Orientation = PropellerOrientation.Positive;
        [Range(0f, 1f)]
        public float RelativeHealth = 1f;
        
        public Motor ConnectedEngine;
        public PropellerDamageData DamageParameters;
        public PropellerData PropellerParameters;

        [HideInInspector]
        public float Angle = 0f;
        [HideInInspector]
        public float InertiaMomentum = 0f;

        [HideInInspector]
        public float ThrustToTorque = 0f;
        [HideInInspector]
        public float OmegaToThrust = 0f;
        [HideInInspector]
        public float OmegaToDrag = 0f;

        [HideInInspector]
        public Vector3 Thrust = Vector3.zero;
        [HideInInspector]
        public Vector3 Drag = Vector3.zero;
        [HideInInspector]
        public Vector3 EccentricForce = Vector3.zero;

        private readonly float _inchToMeter = 0.0254f;  // [inch] -> [meter]

        public void ComputeCoefficients()
        {
            var r = 0.5f * PropellerParameters.Diameter * _inchToMeter;
            var g = 0.5f * PropellerParameters.Pitch * _inchToMeter / Mathf.PI;

            ThrustToTorque = PropellerParameters.TorqueAlpha * PropellerParameters.Pitch * _inchToMeter / 2f / Mathf.PI;
            OmegaToThrust = 0.5f * PropellerParameters.ThrustAlpha * PropellerParameters.BladesNumber * r * g * (r * r - g * g * Mathf.Log(1f + r * r / g / g));
            OmegaToDrag = 0.5f * PropellerParameters.TorqueAlpha * PropellerParameters.ThrustAlpha * PropellerParameters.ViscousAlpha *
                PropellerParameters.BladesNumber * r * g * g * Mathf.Log(1f + r * r / g / g);
            InertiaMomentum = Mass * r * r / 3f;
        }

        public float ComputeExpGamma()
        {
            var epsilon = 0.01f;
            var s = 2.4e6f;
            var c = PropellerParameters.ThrustAlpha;
            var n = PropellerParameters.BladesNumber;
            var omega = ConnectedEngine.AngularVelocity;

            if (Mathf.Abs(omega) < epsilon)
            {
                omega = Mathf.Sign(omega) * epsilon;
            }

            return 1f - Mathf.Exp(-4f * Mathf.Pow(Mathf.PI, 2f) * s / Mathf.Pow(c * n * omega, 2f));
        }

        public void UpdateAngle()
        {
            // Integrate angular velocity
            Angle += Time.fixedDeltaTime * DamageParameters.AngularVelocityCoefficient * ConnectedEngine.AngularVelocity;

            // Check: angle in [-pi, pi]
            if (Angle > 2f * Mathf.PI)
            {
                Angle = Mathf.Abs(Angle) - 2f * Mathf.PI;
            }
            else if (Angle < 0f)
            {
                Angle += 2f * Mathf.PI;
            }
        }

        public void ComputeForces(float density, Vector3 velocity)
        {
            // Compute flow velocity in relative coord system
            Vector3 u = transform.InverseTransformVector(velocity - ConnectedBody.velocity);

            // Prepare variables
            var gammaExp = ComputeExpGamma();
            var gamma = 0.5f * PropellerParameters.Pitch * _inchToMeter / Mathf.PI;
            var omega = ConnectedEngine.AngularVelocity + u.y / gamma;
            var eForce = DamageParameters.EccentricCoefficient * PropellerParameters.Diameter * _inchToMeter / 3f * Mass * 
                Mathf.Pow(ConnectedEngine.AngularVelocity * DamageParameters.AngularVelocityCoefficient, 2f) * (1f - RelativeHealth) * RelativeHealth;

            // Update aerodynamic forces
            u.y = 0f;
            Thrust.y = RelativeHealth * gammaExp * density * OmegaToThrust * omega * Mathf.Abs(omega);
            Drag = RelativeHealth * gammaExp * density * OmegaToDrag * Mathf.Abs(omega) * u;
            Torque.y = ThrustToTorque * Thrust.y;

            // Update mechanical damage force
            EccentricForce.x = eForce * Mathf.Sin(Angle);
            EccentricForce.z = eForce * Mathf.Cos(Angle);
        }

        public void UpdateAerodynamicForce()
        {
            // Initialize
            Thrust = Vector3.zero;
            Torque = Vector3.zero;
            Drag = Vector3.zero;
            EccentricForce = Vector3.zero;

            // Update parameters
            UpdateAngle();
            ComputeForces(AirParameters.Density, AirParameters.Velocity);

            // Update force
            Force = Thrust + EccentricForce;

            switch (Orientation)
            {
                case PropellerOrientation.Positive:
                    Torque = -Torque;
                    break;
                case PropellerOrientation.Negative:
                    break;
                default:
                    Torque = -Torque;
                    break;
            }
        }

        protected override void FixedUpdate()
        {
            UpdateAerodynamicForce();
            ConnectedBody.AddForce(transform.TransformVector(Drag));
            base.FixedUpdate();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            ComputeCoefficients();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Thrust = Vector3.zero;
            Drag = Vector3.zero;
            EccentricForce = Vector3.zero;
            Angle = 0f;
            InertiaMomentum = 0f;
        }
    }
}
