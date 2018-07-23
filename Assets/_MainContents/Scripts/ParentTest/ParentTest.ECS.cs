#if ENABLE_PARENT_TEST
namespace MainContents.ParentTest
{
    using UnityEngine;
    using UnityEngine.Assertions;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Rendering;
    using Unity.Transforms;
    using Unity.Burst;
    using Unity.Jobs;
    using Unity.Collections;


    /// <summary>
    /// 回転情報
    /// </summary>
    public struct DokabenRotationData : IComponentData
    {
        /// <summary>
        /// 経過時間計測用
        /// </summary>
        public float DeltaTimeCounter;

        /// <summary>
        /// コマ数のカウンタ
        /// </summary>
        public int FrameCounter;

        /// <summary>
        /// 1コマに於ける回転角度
        /// </summary>
        public float CurrentAngle;

        /// <summary>
        /// 現在の回転角度
        /// </summary>
        public float CurrentRot;
    }

    /// <summary>
    /// カメラ情報参照用Entity生成用のダミーデータ
    /// </summary>
    public struct SharedCameraData : IComponentData { }

    /// <summary>
    /// カメラの回転情報
    /// </summary>
    public struct CameraPosition : ISharedComponentData
    {
        public float3 Value;
    }


    /// <summary>
    /// ドカベンロゴ回転システム(親子構造版)
    /// </summary>
    [UpdateAfter(typeof(MeshInstanceRendererSystem))]   // MeshInstanceRendererSystemの処理負荷軽減の為に後に回す
    public sealed class ParentTestJobSystem : JobComponentSystem
    {
        /// <summary>
        /// 定数
        /// </summary>
        public sealed class Constants
        {
            /// <summary>
            /// コマ数
            /// </summary>
            public const int Framerate = 9;

            /// <summary>
            /// 1コマに於ける回転角度
            /// </summary>
            public const float Angle = (90f / Framerate); // 90度をコマ数で分割

            /// <summary>
            /// コマ中の待機時間
            /// </summary>
            public const float Interval = 0.2f;
        }

        /// <summary>
        /// カメラ情報参照用Entity
        /// </summary>
        struct SharedCameraDataGroup
        {
            public readonly int Length;
            [ReadOnly] public ComponentDataArray<SharedCameraData> Dummy;   // これはInject用の識別子みたいなもの
            [ReadOnly] public SharedComponentDataArray<CameraPosition> CameraPosition;
        }

        /// <summary>
        /// 回転処理用Job
        /// </summary>
        [BurstCompile]
        struct RotationJob : IJobProcessComponentData<Position, Rotation, DokabenRotationData>
        {
            /// <summary>
            /// カメラの位置情報
            /// </summary>
            public float3 CameraPosition;

            /// <summary>
            /// Time.deltaTime
            /// </summary>
            public float DeltaTime;

            /// <summary>
            /// Job実行
            /// </summary>
            public void Execute(ref Position pos, ref Rotation rot, ref DokabenRotationData dokabenRotData)
            {
                var ret = rot.Value;
                if (dokabenRotData.DeltaTimeCounter >= Constants.Interval)
                {
                    dokabenRotData.CurrentRot += dokabenRotData.CurrentAngle;
                    dokabenRotData.FrameCounter = dokabenRotData.FrameCounter + 1;
                    if (dokabenRotData.FrameCounter >= Constants.Framerate)
                    {
                        dokabenRotData.CurrentAngle = -dokabenRotData.CurrentAngle;
                        dokabenRotData.FrameCounter = 0;
                    }
                    dokabenRotData.DeltaTimeCounter = 0f;
                }
                else
                {
                    dokabenRotData.DeltaTimeCounter += this.DeltaTime;
                }

                // Billboard Quaternion
                var target = pos.Value - this.CameraPosition;
                var billboardQuat = quaternion.lookRotation(target, new float3(0, 1, 0));

                rot.Value = math.mul(billboardQuat, quaternion.rotateX(math.radians(dokabenRotData.CurrentRot)));
            }
        }

        /// <summary>
        /// カメラ情報参照用Entity
        /// </summary>
        [Inject] SharedCameraDataGroup _sharedCameraDataGroup;

        /// <summary>
        /// 回転処理用Job
        /// </summary>
        RotationJob _rotationjob;

        protected override void OnCreateManager(int capacity)
        {
            base.OnCreateManager(capacity);
            this._rotationjob = new RotationJob();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            // 1個しか無い想定
            Assert.IsTrue(this._sharedCameraDataGroup.Length == 1);

            // IJobProcessComponentDataに対しISharedComponentDataを直接渡すことは出来ない?みたいなので、
            // 予めInjectしたカメラの回転情報をScheduleを叩く前に渡した上で実行する
            this._rotationjob.CameraPosition = this._sharedCameraDataGroup.CameraPosition[0].Value;
            this._rotationjob.DeltaTime = Time.deltaTime;
            return this._rotationjob.Schedule(this, 7, inputDeps);
        }
    }
}
#endif
