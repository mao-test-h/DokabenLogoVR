namespace MainContents.MatrixTest
{
    using UnityEngine;
    using Unity.Entities;
    using Unity.Transforms;

#if ENABLE_MATRIX_TEST

    /// <summary>
    /// ドカベンロゴ回転テスト(回転行列演算版)
    /// </summary>
    public sealed class MatrixTest : DokabenTestBase
    {
        /// <summary>
        /// CameraのTransformの参照
        /// </summary>
        [SerializeField] Transform _cameraTrs;

        /// <summary>
        /// EntityManager
        /// </summary>
        EntityManager _entityManager;

        /// <summary>
        /// カメラ情報参照用Entity
        /// </summary>
        Entity _sharedCameraDataEntity;


        /// <summary>
        /// MonoBehaviour.Start
        /// </summary>
        void Start()
        {
            //UnityEngine.XR.XRSettings.eyeTextureResolutionScale = 0.5f;

            var entityManager = World.Active.GetOrCreateManager<EntityManager>();

            // ドカベンロゴのArchetype
            var dokabenLogoArchetype = entityManager.CreateArchetype(
                typeof(AnimationData),
                typeof(TransformMatrix));

            // カメラ情報参照用EntityのArchetype
            var sharedCameraDataArchetype = entityManager.CreateArchetype(
                typeof(SharedCameraData));

            // ドカベンロゴをランダムな位置に生成
            base.CreateEntitiesFromRandomPosition((randomPosition, look) =>
                {
                    var entity = entityManager.CreateEntity(dokabenLogoArchetype);
                    entityManager.SetComponentData(
                        entity,
                        new AnimationData
                        {
                            // 0度~90度の間でランダムに回転させておく
                            AnimationHeader = Random.Range(0, 90),
                            Position = randomPosition,
                        });
                    entityManager.AddSharedComponentData(entity, look);
                });

            // カメラ情報参照用Entityの生成
            var sharedCameraDataEntity = entityManager.CreateEntity(sharedCameraDataArchetype);
            entityManager.SetComponentData(sharedCameraDataEntity, new SharedCameraData());
            entityManager.AddSharedComponentData(sharedCameraDataEntity, new CameraPosition { Value = this._cameraTrs.localPosition, });
            this._sharedCameraDataEntity = sharedCameraDataEntity;
            this._entityManager = entityManager;
        }

        /// <summary>
        /// MonoBehaviour.Update
        /// </summary>
        void Update()
        {
            // こればかりはUpdate内で更新...

            // 但し全てのEntityに対してCamera座標を持たせた上で更新を行う形で実装すると、
            // Update内でとんでもない数のEntityを面倒見無くてはならなくなるので、
            // 予めカメラ情報参照用のEntityを一つだけ生成し、そいつのみに更新情報を渡す形にする。
            // →その上で必要なComponentSystem内でカメラ情報参照用のEntityをInjectして参照すること。
            this._entityManager.SetSharedComponentData(this._sharedCameraDataEntity, new CameraPosition { Value = this._cameraTrs.localPosition, });
        }
    }

#else

    public sealed class MatrixTest : DokabenTestBase
    {
        void Start() { Destroy(this.gameObject); }
    }

#endif
}
