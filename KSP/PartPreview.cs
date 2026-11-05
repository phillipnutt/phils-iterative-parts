using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PIP
{
    // Pictures of installed parts for the program window: one part turning
    // slowly, and still thumbnails for the look picker. A copy of the part's
    // model (meshes only, no modules or colliders) is lit and filmed by a
    // private camera far below the scene. Everything is switched on only for
    // the moment of filming, so no other camera ever sees it.
    public sealed class PartPreview : IDisposable
    {
        const int Layer = 31;
        const int TurnSize = 400, ThumbSize = 96, MaxThumbs = 300;
        const float Fov = 26f, Tilt = 16f, DegreesPerSecond = 22f;

        GameObject root, pivot, holder;
        Camera cam;
        RenderTexture turn;
        GameObject model;          // the part on the turntable
        string modelName, wanted;
        float radius, angle = 35f;
        bool failed;

        readonly Dictionary<string, RenderTexture> thumbs = new Dictionary<string, RenderTexture>();
        readonly List<string> queue = new List<string>();
        readonly HashSet<string> broken = new HashSet<string>();

        // The turntable picture of the part last asked for, or null until it's ready.
        public Texture Turntable => !failed && modelName != null && modelName == wanted ? turn : null;

        public void Want(string partName) => wanted = partName;

        // A still picture of a part, or null while it's being made (or if it can't be).
        public Texture Thumb(string partName)
        {
            if (string.IsNullOrEmpty(partName) || failed || broken.Contains(partName)) return null;
            if (thumbs.TryGetValue(partName, out RenderTexture t) && t != null && t.IsCreated()) return t;
            if (!queue.Contains(partName)) queue.Add(partName);
            return null;
        }

        // Once a frame, before the scene renders.
        public void Tick(bool turning)
        {
            if (failed) return;
            try
            {
                if (root == null) Setup();
                if (turning && wanted != modelName) Load(wanted);
                if (turning && model != null)
                {
                    angle = (angle + DegreesPerSecond * Time.unscaledDeltaTime) % 360f;
                    Film(model, radius, angle, turn);
                }
                if (queue.Count > 0)
                {
                    string name = queue[0];
                    queue.RemoveAt(0);
                    MakeThumb(name);
                }
            }
            catch (Exception e)
            {
                failed = true;
                Debug.LogError("[PIP] Part preview failed, turning it off: " + e);
            }
        }

        void Setup()
        {
            root = new GameObject("PIP part preview");
            root.transform.position = new Vector3(0, -40000, 0);
            pivot = new GameObject("pivot");
            pivot.transform.SetParent(root.transform, false);
            holder = new GameObject("PIP part preview holder");
            holder.SetActive(false);   // copies are made under it, so no part script ever wakes up

            var camGo = new GameObject("camera");
            camGo.transform.SetParent(root.transform, false);
            cam = camGo.AddComponent<Camera>();
            cam.enabled = false;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = PipSkin.Hero;
            cam.cullingMask = 1 << Layer;
            cam.fieldOfView = Fov;
            cam.allowHDR = false;
            cam.allowMSAA = true;
            cam.useOcclusionCulling = false;

            AddLight("key", new Color(1f, 0.97f, 0.92f), 1.05f, Quaternion.Euler(38, -35, 0));
            AddLight("rim", new Color(1f, 0.66f, 0.3f), 0.9f, Quaternion.Euler(12, 160, 0));
            AddLight("fill", new Color(0.55f, 0.65f, 0.9f), 0.35f, Quaternion.Euler(-15, 70, 0));

            turn = NewTexture(TurnSize, 4);
            root.SetActive(false);
        }

        void AddLight(string name, Color color, float intensity, Quaternion rotation)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            go.transform.rotation = rotation;
            Light l = go.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = color;
            l.intensity = intensity;
            l.cullingMask = 1 << Layer;
            l.shadows = LightShadows.None;
        }

        static RenderTexture NewTexture(int size, int aa)
        {
            var t = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32) { antiAliasing = aa, hideFlags = HideFlags.HideAndDontSave };
            t.Create();
            return t;
        }

        void Load(string partName)
        {
            if (model != null) UnityEngine.Object.DestroyImmediate(model);
            model = null;
            modelName = partName;
            if (string.IsNullOrEmpty(partName) || broken.Contains(partName)) return;
            model = Copy(partName, out radius);
            if (model == null) broken.Add(partName);
        }

        void MakeThumb(string partName)
        {
            GameObject copy = Copy(partName, out float r);
            if (copy == null) { broken.Add(partName); return; }
            if (thumbs.Count >= MaxThumbs)
            {
                foreach (RenderTexture old in thumbs.Values) Release(old);
                thumbs.Clear();
            }
            if (model != null) model.SetActive(false);
            RenderTexture t = NewTexture(ThumbSize, 2);
            Film(copy, r, 35f, t);
            thumbs[partName] = t;
            UnityEngine.Object.DestroyImmediate(copy);
            if (model != null) model.SetActive(true);
        }

        void Film(GameObject subject, float r, float yaw, RenderTexture target)
        {
            pivot.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            float distance = r / Mathf.Sin(Fov * 0.5f * Mathf.Deg2Rad) * 1.06f;
            Vector3 centre = pivot.transform.position;
            cam.transform.position = centre + Quaternion.Euler(Tilt, 0, 0) * new Vector3(0, 0, -distance);
            cam.transform.LookAt(centre);
            cam.nearClipPlane = Mathf.Max(0.01f, distance - r * 1.5f);
            cam.farClipPlane = distance + r * 1.5f;
            cam.targetTexture = target;
            root.SetActive(true);
            try { cam.Render(); }
            finally
            {
                root.SetActive(false);
                cam.targetTexture = null;
            }
        }

        // A copy of the part's model with only what draws, centred on the pivot.
        GameObject Copy(string partName, out float r)
        {
            r = 0;
            Part prefab = PartLoader.getPartInfoByName(partName)?.partPrefab;
            Transform source = prefab == null ? null : prefab.transform.Find("model");
            if (source == null) return null;

            GameObject go = UnityEngine.Object.Instantiate(source.gameObject, holder.transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;

            // Keep meshes; drop scripts, colliders, lights and effects.
            for (int pass = 0; pass < 2; pass++)
                foreach (Component c in go.GetComponentsInChildren<Component>(true).Reverse())
                {
                    if (c == null) continue;
                    if (c is MonoBehaviour || c is Collider) UnityEngine.Object.DestroyImmediate(c);
                    else if (c is Light l) l.enabled = false;
                    else if (c is Renderer pr && pr.GetType().Name == "ParticleSystemRenderer") pr.enabled = false;
                }

            // Parts with variants: show only the default variant's meshes.
            ModulePartVariants variants = prefab.FindModuleImplementing<ModulePartVariants>();
            PartVariant variant = variants?.SelectedVariant ?? variants?.variantList?.FirstOrDefault();
            if (variant?.InfoGameObjects != null)
                foreach (PartGameObjectInfo info in variant.InfoGameObjects)
                    foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
                        if (t.name == info.Name) t.gameObject.SetActive(info.Status);

            // Engine shrouds hide the nozzle; show the engine as it looks with nothing below it.
            var shrouds = new HashSet<string>();
            foreach (ModuleJettison j in prefab.FindModulesImplementing<ModuleJettison>())
                foreach (string n in (j.jettisonName ?? "").Split(',')) if (n.Trim().Length > 0) shrouds.Add(n.Trim());
            if (shrouds.Count > 0)
                foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
                    if (shrouds.Contains(t.name)) t.gameObject.SetActive(false);

            foreach (Transform t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = Layer;

            // Size and centre, from the meshes that show.
            bool any = false;
            var bounds = new Bounds();
            foreach (Renderer rend in go.GetComponentsInChildren<Renderer>(true))
            {
                if (!rend.enabled || !Shown(rend.transform, go.transform) || rend.GetType().Name == "ParticleSystemRenderer") continue;
                Mesh mesh = rend is SkinnedMeshRenderer skin ? skin.sharedMesh : rend.GetComponent<MeshFilter>()?.sharedMesh;
                if (mesh == null) continue;
                Bounds b = mesh.bounds;
                Matrix4x4 m = go.transform.worldToLocalMatrix * rend.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = m.MultiplyPoint3x4(b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)));
                    if (!any) { bounds = new Bounds(corner, Vector3.zero); any = true; }
                    else bounds.Encapsulate(corner);
                }
            }
            if (!any) { UnityEngine.Object.DestroyImmediate(go); return null; }

            Vector3 scale = go.transform.localScale;
            Vector3 centre = Vector3.Scale(bounds.center, scale);
            r = Mathf.Max(0.05f, Vector3.Scale(bounds.extents, scale).magnitude);
            go.transform.SetParent(pivot.transform, false);
            go.transform.localPosition = -centre;
            go.transform.localRotation = Quaternion.identity;
            return go;
        }

        static bool Shown(Transform t, Transform top)
        {
            for (; t != null; t = t.parent)
            {
                if (!t.gameObject.activeSelf) return false;
                if (t == top) return true;
            }
            return true;
        }

        static void Release(RenderTexture t)
        {
            if (t == null) return;
            t.Release();
            UnityEngine.Object.Destroy(t);
        }

        public void Dispose()
        {
            foreach (RenderTexture t in thumbs.Values) Release(t);
            thumbs.Clear();
            Release(turn);
            if (root != null) UnityEngine.Object.Destroy(root);
            if (holder != null) UnityEngine.Object.Destroy(holder);
            root = holder = null;
        }
    }
}
