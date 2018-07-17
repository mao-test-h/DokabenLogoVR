namespace MainContents.DebugUtility
{
    using UnityEngine;
    using UnityEngine.UI;

    [RequireComponent(typeof(Text))]
    public sealed class FPSCounter : MonoBehaviour
    {
        const float FPSMeasurePeriod = 0.5f;
        Text _textFpsCount;
        int _fpsAccumulator = 0;
        float _fpsNextPeriod = 0;
        int _currentFps;

        void Start()
        {
            this._textFpsCount = this.GetComponent<Text>();
#if !ENABLE_DEBUG
            Destroy(this._textFpsCount.canvas.gameObject);
#else
            this._fpsNextPeriod = Time.realtimeSinceStartup + FPSMeasurePeriod;
#endif
        }

        void Update()
        {
            // measure average frames per second
            this._fpsAccumulator++;
            if (Time.realtimeSinceStartup > this._fpsNextPeriod)
            {
                this._currentFps = (int)(this._fpsAccumulator / FPSMeasurePeriod);
                this._fpsAccumulator = 0;
                this._fpsNextPeriod += FPSMeasurePeriod;
                this._textFpsCount.text = this._currentFps.ToString();
            }
        }
    }
}
