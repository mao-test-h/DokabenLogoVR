#if ENABLE_MATRIX_TEST || ENABLE_LIGHT_MATRIX_TEST
namespace MainContents.MatrixTest
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
    /// アニメーションデータ
    /// </summary>
    public struct AnimationData : IComponentData
    {
        /// <summary>
        /// アニメーションテーブル内に於ける再生位置
        /// </summary>
        public int AnimationHeader;

        /// <summary>
        /// 位置
        /// </summary>
        public float3 Position;
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


#if ENABLE_MATRIX_TEST
    /// <summary>
    /// ドカベンロゴ回転システム(回転行列演算版)
    /// </summary>
    [UpdateAfter(typeof(MeshInstanceRendererSystem))]   // MeshInstanceRendererSystemの処理負荷軽減の為に後に回す
    public sealed class MatrixTestJobSystem : JobComponentSystem
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
            /// math.sin ルックアップテーブル
            /// </summary>
            [ReadOnly] NativeArray<float> SinTable;


            /// <summary>
            /// コンストラクタ
            /// </summary>
            /// <param name="animationTable">アニメーションテーブル</param>
            /// <param name="sinTable">math.sin ルックアップテーブル</param>
            public RotateJob(NativeArray<int> animationTable, NativeArray<float> sinTable)
            {
                this.AnimationTable = animationTable;
                this.SinTable = sinTable;

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
                float y = 0f, z = 0f;
                float halfY = y - 0.5f;
                float sin = this.Sin(angle);
                float cos = this.Cos(angle);
                axisRotationMatrix.c1.yz = new float2(cos, sin);
                axisRotationMatrix.c2.yz = new float2(-sin, cos);
                axisRotationMatrix.c3.yz = new float2(halfY - halfY * cos + z * sin, z - halfY * sin - z * cos);

                // 最後にビルドード+移動行列と軸回転行列を掛け合わせる
                var ret = math.mul(new float4x4(billboardQuat, data.Position), axisRotationMatrix);

                // 計算結果の反映
                transform.Value = ret;
            }

            float Sin(int deg)
            {
                // 0~90(度)
                if (deg <= 90) { return this.SinTable[deg]; }
                // 90~180
                else if (deg <= 180) { return this.SinTable[180 - deg]; }
                // 180~270
                else if (deg <= 270) { return -this.SinTable[deg - 180]; }
                // 270~360
                else { return -this.SinTable[360 - deg]; }
            }

            float Cos(int deg)
            {
                return Sin(deg + 90);
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

        /// <summary>
        /// math.sin ルックアップテーブル
        /// </summary>
        NativeArray<float> SinTable;

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

            // sin・cosのルックアップテーブルを作成してJobに渡す
            const int Length = 90;
            this.SinTable = new NativeArray<float>(Length + 1, Allocator.Persistent);
            for (int i = 0; i <= Length; ++i)
            {
                this.SinTable[i] = math.sin(math.radians(i));
            }
            this._rotateJob = new RotateJob(this.AnimationTable, this.SinTable);
        }

        protected override void OnDestroyManager()
        {
            if (this.AnimationTable.IsCreated) { this.AnimationTable.Dispose(); }
            if (this.SinTable.IsCreated) { this.SinTable.Dispose(); }
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
#endif
}
#endif
