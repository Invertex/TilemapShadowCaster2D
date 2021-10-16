using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace Invertex.Unity.URP
{
    [ExecuteAlways, ExecuteInEditMode, DisallowMultipleComponent,
        RequireComponent(typeof(CompositeCollider2D), typeof(TilemapCollider2D))]
    public class TilemapShadowCaster2D : MonoBehaviour
    {
        public bool useRendererSilhouette = true;
        [Tooltip("Should the Tilemap cast shadows onto itself?")]
        public bool selfShadows = false;
        [Tooltip("Treat all parts of the Tilemap as one large caster, instead of individual islands casting onto eachother.")]
        public bool useCompositeShadowCaster = false;

        [SerializeField, HideInInspector] CompositeCollider2D compositeCollider;
        [SerializeField, HideInInspector] TilemapCollider2D tilemapCollider;
        [SerializeField, HideInInspector] Rigidbody2D rb2D;

        private readonly List<ShadowCaster2D> shadowCasterPaths = new List<ShadowCaster2D>(32);
        private readonly List<Vector2> pathBuffer = new List<Vector2>(256);

        static readonly FieldInfo boundingSpherefield = typeof(ShadowCaster2D).GetField("m_ProjectedBoundingSphere", BindingFlags.NonPublic | BindingFlags.Instance);
        static readonly MethodInfo generateShadowMeshMethod = typeof(ShadowCaster2D).Assembly.GetType("UnityEngine.Rendering.Universal.ShadowUtility").GetMethod("GenerateShadowMesh", BindingFlags.Public | BindingFlags.Static);

        /// <summary>
        /// Generates a shadow shape path from the CompositeCollider2D component and assigns it to a ShadowCaster2D component
        /// Can call this code during runtime if you made runtime changes to the Tilemap and wnat to update the shadows.
        /// </summary>
        public void Regenerate()
        {
            tilemapCollider.usedByComposite = true;
            var compositePaths = compositeCollider.pathCount;

            CleanupShadowList(compositePaths);

            //Calculate shadow paths for each composite collider island
            for (int i = 0; i < compositePaths; i++)
            {
                pathBuffer.Clear();
                int pathLength = compositeCollider.GetPath(i, pathBuffer);
                var shadowPathBuffer = new Vector3[pathLength];

                //Have to cast the Vector2 into Vector3 buffer
                for (int j = 0; j < pathLength; j++)
                {
                    shadowPathBuffer[j] = pathBuffer[j];
                }

                if (shadowCasterPaths.Count - 1 < i)
                {
                    shadowCasterPaths.Add(CreateShadowCasterObj());
                }

                var shadowCaster = shadowCasterPaths[i];
                shadowCaster.selfShadows = selfShadows;
                shadowCaster.useRendererSilhouette = useRendererSilhouette;

                var boundingSphere = generateShadowMeshMethod.Invoke(shadowCaster, new object[] { shadowCaster.mesh, shadowPathBuffer });
                boundingSpherefield.SetValue(shadowCaster, boundingSphere);
            }

            TryApplyCompositeShadows(compositePaths);
        }

        private void DestroySafe(Object obj)
        {
            if (Application.isPlaying) { Destroy(obj); }
            else { DestroyImmediate(obj); }
        }

        private ShadowCaster2D CreateShadowCasterObj()
        {
            var newCasterObj = new GameObject("TMShadowCaster");
            newCasterObj.transform.parent = this.transform;
            newCasterObj.transform.localPosition = Vector3.zero;
            return newCasterObj.AddComponent<ShadowCaster2D>();
        }

        private void TryApplyCompositeShadows(int shadowPaths)
        {
            var compositeShadowCaster = GetComponent<CompositeShadowCaster2D>();

            if (compositeShadowCaster != null && !useCompositeShadowCaster) { DestroySafe(compositeShadowCaster); }
            else if (useCompositeShadowCaster && compositeShadowCaster == null && shadowPaths > 1)
            {
                gameObject.AddComponent<CompositeShadowCaster2D>();
            }
        }

        private void CleanupShadowList(int pathCount)
        {
            shadowCasterPaths.Clear();
            shadowCasterPaths.AddRange(GetComponentsInChildren<ShadowCaster2D>());

            int shadowPaths = shadowCasterPaths.Count;

            if (pathCount < shadowPaths)
            {
                for (int p = shadowPaths - 1; p >= pathCount; p--)
                {
                    DestroySafe(shadowCasterPaths[p].gameObject);
                }

                shadowCasterPaths.RemoveRange(pathCount, shadowPaths - pathCount);
            }
        }

        private void Reinitialize()
        {
            rb2D = GetComponent<Rigidbody2D>();
            compositeCollider = GetComponent<CompositeCollider2D>();
            tilemapCollider = GetComponent<TilemapCollider2D>();
            Regenerate();
        }

        //Need to regenerate Shadow data after script recompilation for some reason... 
        void OnEnable()
        {
            Reinitialize();
        }

        private void Reset()
        {
            Reinitialize();
            if (rb2D.bodyType == RigidbodyType2D.Dynamic) { rb2D.bodyType = RigidbodyType2D.Static; }
        }
    }
}
