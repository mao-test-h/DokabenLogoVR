#if ENABLE_LIGHT_MATRIX_TEST
namespace MainContents.LightMatrixTest
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

    using MainContents.MatrixTest;

    /// <summary>
    /// ドカベンロゴ回転システム(回転行列演算版)
    /// </summary>
    [UpdateAfter(typeof(MeshInstanceRendererSystem))]   // MeshInstanceRendererSystemの処理負荷軽減の為に後に回す
    public sealed class LightMatrixTestJobSystem : JobComponentSystem
    {
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
        /// 回転行列演算job
        /// </summary>
        [BurstCompile]
        struct RotateJob : IJobProcessComponentData<AnimationData, TransformMatrix>
        {
            /// <summary>
            /// アニメーションの再生速度
            /// </summary>
            public const float AnimationSpeed = 1f;

            /// <summary>
            /// Time.time
            /// </summary>
            public float Time;

            /// <summary>
            /// カメラの位置情報
            /// </summary>
            public CameraPosition CameraPosition;

            /// <summary>
            /// アニメーションテーブル
            /// </summary>
            [ReadOnly] NativeArray<int> AnimationTable;


            /// <summary>
            /// コンストラクタ
            /// </summary>
            /// <param name="animationTable">アニメーションテーブル</param>
            public RotateJob(NativeArray<int> animationTable)
            {
                this.AnimationTable = animationTable;

                // その他フィールドはダミーを入れておく
                this.Time = 0f;
                this.CameraPosition = new CameraPosition();
            }

            /// <summary>
            /// Job実行
            /// </summary>
            public void Execute(ref AnimationData data, ref TransformMatrix transform)
            {
                // Billboard Quaternion
                var target = data.Position - this.CameraPosition.Value;
                var billboardQuat = quaternion.lookRotation(target, new float3(0, 1, 0));

                // 軸回転行列
                float4x4 axisRotationMatrix = float4x4.identity;

                // 時間の正弦を算出(再生位置を加算することで角度をずらせるように設定)
                int sinIndex = (int)math.degrees((this.Time * RotateJob.AnimationSpeed) + data.AnimationHeader) % 360;
                float sinTime = this.Sin(sinIndex);

                // _SinTime0~1に正規化→0~15(コマ数分)の範囲にスケールして要素数として扱う
                float normal = (sinTime + 1f) / 2f;

                // X軸に0~90度回転
                var animIndex = (int)math.round(normal * (this.AnimationTable.Length - 1));
                int angle = this.AnimationTable[animIndex];

                // 任意の原点周りにX軸回転を行う(原点を-0.5ずらして下端に設定)
                float sin = this.Sin(angle);
                float cos = this.Cos(angle);
                axisRotationMatrix.c1.yz = new float2(cos, sin);
                axisRotationMatrix.c2.yz = new float2(-sin, cos);

                // 最後にビルドード+移動行列と軸回転行列を掛け合わせる
                var ret = math.mul(new float4x4(billboardQuat, data.Position), axisRotationMatrix);

                // 計算結果の反映
                transform.Value = ret;
            }

            float Sin(int deg)
            {
                // 0~90(度)
                if (deg <= 90) { return Cos(deg - 90); }
                // 90~180
                else if (deg <= 180) { return Cos((180 - deg) - 90); }
                // 180~270
                else if (deg <= 270) { return -Cos((deg - 180) - 90); }
                // 270~360
                else { return -Cos((360 - deg) - 90); }
            }

            float Cos(int deg)
            {
                float rad = deg * ((3.14f * 2) / 360f);
                // math.powで冪乗を算出すると激重になる
                // →代わりに全部乗算だけで完結させると速い
                float pow1 = rad * rad;
                float pow2 = pow1 * pow1;
                float pow3 = pow2 * pow1;
                float pow4 = pow2 * pow2;
                // 階乗は算出コストを省くために数値リテラルで持つ
                float ret = 1 - (pow1 / 2f)
                            + (pow2 / 24f)        // 4!
                            - (pow3 / 720f)       // 6!
                            + (pow4 / 40320f);    // 8!
                return ret;
            }
        }

        /// <summary>
        /// カメラ情報参照用Entity
        /// </summary>
        [Inject] SharedCameraDataGroup _sharedCameraDataGroup;

        /// <summary>
        /// 回転行列演算job
        /// </summary>
        RotateJob _rotateJob;

        /// <summary>
        /// アニメーションテーブル
        /// </summary>
        NativeArray<int> AnimationTable;

        protected override void OnCreateManager(int capacity)
        {
            base.OnCreateManager(capacity);

            // 90度～0度の回転アニメーションテーブルの生成
            const int AnimationTableLength = 16;
            this.AnimationTable = new NativeArray<int>(AnimationTableLength, Allocator.Persistent);
            for (int i = 0; i <= AnimationTableLength - 1; ++i)
            {
                AnimationTable[i] = (90 - (90 / (AnimationTableLength - 1) * i));
            }
            this._rotateJob = new RotateJob(this.AnimationTable);
        }

        protected override void OnDestroyManager()
        {
            if (this.AnimationTable.IsCreated) { this.AnimationTable.Dispose(); }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            // 1個しか無い想定
            Assert.IsTrue(this._sharedCameraDataGroup.Length == 1);

            // Jobの実行
            this._rotateJob.Time = Time.time;
            this._rotateJob.CameraPosition = this._sharedCameraDataGroup.CameraPosition[0];
            return this._rotateJob.Schedule(this, 7, inputDeps);
        }
    }
}
#endif
