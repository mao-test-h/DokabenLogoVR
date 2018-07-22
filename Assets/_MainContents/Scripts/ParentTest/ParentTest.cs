namespace MainContents.ParentTest
{
    using UnityEngine;
    using Unity.Entities;
    using Unity.Transforms;
    using Unity.Mathematics;

#if ENABLE_PARENT_TEST

    /// <summary>
    /// ドカベンロゴ回転テスト(親子構造版)
    /// </summary>
    public sealed class ParentTest : DokabenTestBase
    {
        /// <summary>
        /// 子オブジェクトのオフセット
        /// </summary>
        [SerializeField] Vector3 _childOffset;

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

            // 親Entityのアーキタイプ
            var parentArchetype = entityManager.CreateArchetype(
                typeof(DokabenRotationData),
                typeof(Position),
                typeof(Rotation),
                typeof(TransformMatrix));

            // 子Entityのアーキタイプ
            var childArchetype = entityManager.CreateArchetype(
                typeof(LocalPosition),
                typeof(LocalRotation),
                typeof(TransformParent),
                typeof(TransformMatrix));

            // カメラ情報参照用Entityの生成
            var sharedCameraDataArchetype = entityManager.CreateArchetype(
                typeof(SharedCameraData));

            // ドカベンロゴをランダムな位置に生成
            base.CreateEntitiesFromRandomPosition((randomPosition, look) =>
                {
                    // 親Entityの生成
                    var parentEntity = entityManager.CreateEntity(parentArchetype);
                    entityManager.SetComponentData(parentEntity, new Position { Value = randomPosition });
                    entityManager.SetComponentData(parentEntity, new Rotation { Value = quaternion.identity });
                    entityManager.SetComponentData(parentEntity, new DokabenRotationData
                    {
                        CurrentAngle = ParentTestJobSystem.Constants.Angle,
                        DeltaTimeCounter = 0f,
                        FrameCounter = 0,
                        CurrentRot = 0f,
                    });

                    // 子Entityの生成
                    var childEntity = entityManager.CreateEntity(childArchetype);
                    entityManager.SetComponentData(childEntity, new LocalPosition { Value = this._childOffset });
                    entityManager.SetComponentData(childEntity, new LocalRotation { Value = quaternion.identity });
                    entityManager.SetComponentData(childEntity, new TransformParent { Value = parentEntity });
                    entityManager.AddSharedComponentData(childEntity, look);
                });

            // カメラ情報参照用Entityの生成
            var sharedCameraDataEntity = entityManager.CreateEntity(sharedCameraDataArchetype);
            entityManager.SetComponentData(sharedCameraDataEntity, new SharedCameraData());
            entityManager.AddSharedComponentData(sharedCameraDataEntity, new CameraRotation { Value = this.GetBillboardRotation(this._cameraTrs.rotation) });
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
            this._entityManager.SetSharedComponentData(this._sharedCameraDataEntity, new CameraRotation { Value = this.GetBillboardRotation(this._cameraTrs.rotation) });
        }

        quaternion GetBillboardRotation(Quaternion rot)
        {
            var euler = rot.eulerAngles;
            return Quaternion.Euler(new Vector3(euler.x, euler.y, 0f));
        }
    }

#else

    public sealed class ParentTest : DokabenTestBase
    {
        void Start() { Destroy(this.gameObject); }
    }

#endif
}
