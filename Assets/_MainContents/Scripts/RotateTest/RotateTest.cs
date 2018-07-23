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
            var rotateLooka = this.CreateMeshInstanceRenderer(base._dokabenRenderData);
            base.CreateEntitiesFromRandomPosition((randomPosition, look) =>
                {
                    // 親Entityの生成
                    var entity = entityManager.CreateEntity(dokabenLogoArchetype);
                    entityManager.SetComponentData(entity, new Position { Value = randomPosition });
                    entityManager.SetComponentData(entity, new Rotation { Value = quaternion.identity });
                    entityManager.SetComponentData(entity, new DokabenRotationData
                    {
                        CurrentAngle = RotateTestJobSystem.Constants.Angle,
                        DeltaTimeCounter = 0f,
                        FrameCounter = 0,
                        CurrentRot = 0f,
                    });
                    entityManager.AddSharedComponentData(entity, rotateLooka);
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

        /// <summary>
        /// MeshInstanceRendererの生成
        /// </summary>
        /// <param name="data">表示データ</param>
        /// <returns>生成したMeshInstanceRenderer</returns>
        public MeshInstanceRenderer CreateMeshInstanceRenderer(MeshInstanceRendererData data)
        {
            // Sprite to Mesh
            var mesh = new Mesh();
            var sprite = data.Sprite;

            var vertices = Array.ConvertAll(data.Sprite.vertices, _ => (Vector3)_).ToList();
            Matrix4x4 mat = Matrix4x4.TRS(
                base._childOffset,
                Quaternion.identity,
                Vector3.one);
            for (int i = 0; i < vertices.Count; ++i)
            {
                vertices[i] = mat.MultiplyPoint3x4(vertices[i]);
            }
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, sprite.uv.ToList());
            mesh.SetTriangles(Array.ConvertAll(sprite.triangles, _ => (int)_), 0);

            var matInst = new Material(data.Material);

            // 渡すマテリアルはGPU Instancingに対応させる必要がある
            var meshInstanceRenderer = new MeshInstanceRenderer();
            meshInstanceRenderer.mesh = mesh;
            meshInstanceRenderer.material = matInst;
            return meshInstanceRenderer;
        }
    }

#else

    public sealed class RotateTest : DokabenTestBase
    {
        void Start() { Destroy(this.gameObject); }
    }

#endif
}
