namespace MainContents
{
    using UnityEngine;
    using UnityEngine.Events;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Rendering;

    /// <summary>
    /// ドカベンロゴテスト ベース
    /// </summary>
    public abstract class DokabenTestBase : MonoBehaviour
    {
        /// <summary>
        /// ドカベン表示用データ
        /// </summary>
        [SerializeField] protected MeshInstanceRendererData _dokabenRenderData;

        /// <summary>
        /// 表示領域のサイズ
        /// </summary>
        [SerializeField] Vector3 _boundSize = new Vector3(256f, 256f, 256f);

        /// <summary>
        /// 最大オブジェクト数
        /// </summary>
        [SerializeField] int _maxObjectNum = 100000;

        /// <summary>
        /// CameraのTransformの参照
        /// </summary>
        [SerializeField] protected Transform _cameraTransform;

        /// <summary>
        /// 子オブジェクトのオフセット
        /// </summary>
        [SerializeField] protected Vector3 _childOffset = new Vector3(0f, 0.65f, 0f);

        /// <summary>
        /// EntityManager
        /// </summary>
        protected EntityManager _entityManager;

        /// <summary>
        /// カメラ情報参照用Entity
        /// </summary>
        protected Entity _sharedCameraDataEntity;


        /// <summary>
        /// Entityをランダムな位置に生成
        /// </summary>
        /// <param name="onCreateEntity">Entity生成毎に呼ばれるコールバック</param>
        protected void CreateEntitiesFromRandomPosition(UnityAction<float3, MeshInstanceRenderer> onCreateEntity)
        {
            var look = Utility.CreateMeshInstanceRenderer(this._dokabenRenderData);
            var halfX = this._boundSize.x / 2;
            var halfY = this._boundSize.y / 2;
            var halfZ = this._boundSize.z / 2;
            for (int i = 0; i < this._maxObjectNum; ++i)
            {
                var randomPos = new float3(Random.Range(-halfX, halfX), Random.Range(-halfY, halfY), Random.Range(-halfZ, halfZ));
                onCreateEntity(randomPos, look);
            }
        }
    }
}
