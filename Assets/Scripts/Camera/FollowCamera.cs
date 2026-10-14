using UnityEngine;
using QuantumCrossy.Core;

namespace QuantumCrossy.CameraRig
{
    /// <summary>
    /// 플레이어를 뒤·위에서 따라간다. 세로 비율을 기준으로 맞추고, 창이 넓어지면 뒤로 빠진다.
    /// </summary>
    public sealed class FollowCamera : MonoBehaviour
    {
        Transform target;
        QuantumCrossyConfig config;
        Camera cam;
        Vector3 velocity;
        bool hasPosition;

        public void Initialize(Transform target, QuantumCrossyConfig config)
        {
            this.target = target;
            this.config = config;
            cam = GetComponent<Camera>();
            Snap();
        }

        void LateUpdate()
        {
            if (target == null || config == null)
                return;

            Vector3 desired = DesiredPosition();
            if (!hasPosition)
            {
                transform.position = desired;
                velocity = Vector3.zero;
                hasPosition = true;
            }
            else
            {
                transform.position = Vector3.SmoothDamp(
                    transform.position,
                    desired,
                    ref velocity,
                    config.cameraSmoothTime);
            }

            if (cam != null)
                cam.fieldOfView = CurrentFov();
            LookAtAnchor();
        }

        public void Snap()
        {
            if (target == null || config == null)
                return;
            transform.position = DesiredPosition();
            velocity = Vector3.zero;
            hasPosition = true;
            if (cam == null)
                cam = GetComponent<Camera>();
            if (cam != null)
                cam.fieldOfView = CurrentFov();
            LookAtAnchor();
        }

        void LookAtAnchor()
        {
            Vector3 look = Anchor() + new Vector3(-config.cameraLookLeft, 0.15f, config.cameraLookAhead);
            Vector3 forward = look - transform.position;
            if (forward.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        Vector3 Anchor()
        {
            Vector3 anchor = target.position;
            anchor.y = config.playerBaseY;
            return anchor;
        }

        Vector3 DesiredPosition()
        {
            float t = AspectT();
            float back = Mathf.Lerp(config.portraitBack, config.landscapeBack, t);
            float height = Mathf.Lerp(config.portraitHeight, config.landscapeHeight, t);
            float side = Mathf.Lerp(config.portraitSide, config.landscapeSide, t);
            return Anchor() + new Vector3(side, height, -back);
        }

        float CurrentFov()
        {
            return Mathf.Lerp(config.portraitFov, config.landscapeFov, AspectT());
        }

        float AspectT()
        {
            float aspect = Screen.height > 0
                ? (float)Screen.width / Screen.height
                : config.portraitAspect;
            return Mathf.InverseLerp(config.portraitAspect, config.landscapeAspect, aspect);
        }
    }
}
