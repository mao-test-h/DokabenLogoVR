namespace MainContents.RotateTest
{
#if ENABLE_ROTATE_TEST
    using System;
    using System.Linq;
    using UnityEngine;
    using Unity.Entities;
    using Unity.Transforms;
    using Unity.Mathematics;
    using Unity.Rendering;

    /// <summary>
    /// ドカベンロゴ回転テスト(非親子構造版)
    /// </summary>
    public sealed class RotateTest : DokabenTestBase
    {
        /// <summary>
        /// MonoBehaviour.Start
        /// </summary>
        void Start()
        {
            var entityManager = World.Active.GetOrCreateManager<EntityManager>();

            // ドカベンロゴのArchetype
            var dokabenLogoArchetype = entityManager.CreateArchetype(
                typeof(DokabenRotationData),
                typeof(Position),
                typeof(Rotation),
                typeof(TransformMatrix));

            // カメラ情報参照用Entityの生成
            var sharedCameraDataArchetype = entityManager.CreateArchetype(
                typeof(SharedCameraData));

            // ドカベンロゴをランダムな位置に生成
            var rotateLook = Utility.CreateMeshInstanceRenderer(base._dokabenRenderData, base._childOffset);
            base.CreateEntitiesFromRandomPosition((randomPosition, look) =>
                {
                    // 親Entityの生成
                    var entity = entityManager.CreateEntity(dokabenLogoArchetype);
                    entityManager.SetComponentData(entity, new Position { Value = randomPosition });
                    entityManager.SetComponentData(entity, new Rotation { Value = quaternion.identity });

                    int currentFrame = UnityEngine.Random.Range(0, RotateTestJobSystem.Constants.Framerate);
                    float currentRot = RotateTestJobSystem.Constants.Angle * currentFrame;
                    entityManager.SetComponentData(entity, new DokabenRotationData
                    {
                        CurrentAngle = RotateTestJobSystem.Constants.Angle,
                        DeltaTimeCounter = 0f,
                        FrameCounter = currentFrame,
                        CurrentRot = currentRot,
                    });
                    entityManager.AddSharedComponentData(entity, rotateLook);
                });

            // カメラ情報参照用Entityの生成
            var sharedCameraDataEntity = entityManager.CreateEntity(sharedCameraDataArchetype);
            entityManager.SetComponentData(sharedCameraDataEntity, new SharedCameraData());
            entityManager.AddSharedComponentData(sharedCameraDataEntity, new CameraPosition { Value = base._cameraTransform.localPosition });
            base._sharedCameraDataEntity = sharedCameraDataEntity;

            base._entityManager = entityManager;
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
            base._entityManager.SetSharedComponentData(base._sharedCameraDataEntity, new CameraPosition { Value = base._cameraTransform.localPosition });
        }
    }

#else

    public sealed class RotateTest : DokabenTestBase
    {
        void Start() { Destroy(this.gameObject); }
    }

#endif
}
