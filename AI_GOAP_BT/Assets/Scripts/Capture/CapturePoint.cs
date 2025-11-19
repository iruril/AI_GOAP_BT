using FSM;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using MEC;

namespace CapturePoint
{
    public enum CaptureState
    {
        Neutral,
        CapturedByBlue,
        CapturedByRed
    }

    public class CapturePoint : StateManager<CaptureState>
    {
        private CapturePoint _context => this;
        private HashSet<Stat> blues = new(32);
        private HashSet<Stat> reds = new(32);
        private Material decalMat;

        [Header("Neutral Color")]
        [SerializeField]
        private Color def = Color.white;

        [Header("Blue Color")]
        [SerializeField]
        private Color blue = Color.blue;

        [Header("Red Color")]
        [SerializeField]
        private Color red = Color.red;

        [Header("Capture Amount/s of Agent")]
        [SerializeField] private float captureAmount = 1.666f;
        public float CaptureAmount => captureAmount;

        public float CaptureGauge { get; set; }
        public CaptureState GetCurrentState() => CurrentState.StateKey;

        private CoroutineHandle colorHandle;

        void Awake()
        {
            decalMat = new Material(GetComponent<DecalProjector>().material);
            GetComponent<DecalProjector>().material = decalMat;
            InitializeStates();
            CurrentState = States[CaptureState.Neutral];
        }

        protected override void Update()
        {
            base.Update();
        }

        private void InitializeStates()
        {
            States.Add(CaptureState.Neutral, new Neutral(_context, CaptureState.Neutral));
            States.Add(CaptureState.CapturedByBlue, new CapturedByBlue(_context, CaptureState.CapturedByBlue));
            States.Add(CaptureState.CapturedByRed, new CapturedByRed(_context, CaptureState.CapturedByRed));
        }

        public int CaptureAmountThreshold()
        {
            return Mathf.Clamp(blues.Count - reds.Count, -5, 5);
        }

        public void AddIntruder(Stat intruder)
        {
            if (intruder.CompareTag("TeamBlue"))
            {
                blues.Add(intruder);
            }
            else
            {
                reds.Add(intruder);
            }
        }

        public void RemoveIntruder(Stat intruder)
        {
            if (intruder.CompareTag("TeamBlue"))
            {
                blues.Remove(intruder);
            }
            else
            {
                reds.Remove(intruder);
            }
        }

        public bool IsEmptyPlace()
        {
            return blues.Count == 0 && reds.Count == 0;
        }

        /// <summary>
        /// 1이면 Blue, 0이면 중립, -1이면 Red.
        /// </summary>
        /// <param name="value"> 1, 0, -1 중 택할것. </param>
        public void UpdateDecalColor(int value)
        {
            Timing.KillCoroutines(colorHandle);

            switch (value)
            {
                case -1:
                    colorHandle = Timing.RunCoroutine(DecalColorLerp(red, 0.25f));
                    break;
                case 0:
                    colorHandle = Timing.RunCoroutine(DecalColorLerp(def, 0.25f));
                    break;
                case 1:
                    colorHandle = Timing.RunCoroutine(DecalColorLerp(blue, 0.25f));
                    break;
            }
        }

        private IEnumerator<float> DecalColorLerp(Color targetCol, float time)
        {
            float t = 0;
            Color currCol = decalMat.GetColor("_BaseColor");

            while(t < time)
            {
                t += Time.deltaTime;
                float lerpT = Mathf.Clamp01(t / time);

                lerpT = lerpT * lerpT * lerpT * (lerpT * (6f * lerpT - 15f) + 10f); //5차 SmoothStep

                Color newCol = Color.Lerp(currCol, targetCol, lerpT);

                decalMat.SetColor("_BaseColor", newCol);

                yield return Timing.WaitForOneFrame;
            }

            decalMat.SetColor("_BaseColor", targetCol);
        }

        public bool NeedToCapture(Transform agent)
        {
            bool result = true;
            if (agent.CompareTag("TeamBlue"))
            {
                if (CurrentState.StateKey == CaptureState.CapturedByBlue) result = false;
            }
            else
            {
                if (CurrentState.StateKey == CaptureState.CapturedByRed) result = false;
            }
            return result;
        }
    }
}
