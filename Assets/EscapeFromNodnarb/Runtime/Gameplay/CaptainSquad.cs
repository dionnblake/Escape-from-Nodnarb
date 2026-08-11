using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EscapeFromNodnarb
{
    public sealed class CaptainSquad
    {
        private const string CaptainPrefabResourcePath = "Captain/CaptainVisual";
        private const string CaptainSourceResourcePath = "Captain/Captain_Unity";
        private const float ImportedCaptainVisualScale = 0.92f;
        private const float ImportedSoldierVisualScale = 2.0f;
        private readonly Transform owner;
        private readonly List<Transform> soldiers = new List<Transform>();
        private readonly List<Vector3> soldierBaseScales = new List<Vector3>();
        private readonly List<Transform> muzzles = new List<Transform>();
        private readonly List<Transform> muzzleFlashes = new List<Transform>();
        private readonly List<Vector3> muzzleFlashBaseScales = new List<Vector3>();
        private GameObject root;
        private Transform captain;
        private Transform captainMuzzle;
        private Vector3 captainBaseScale = Vector3.one;
        private readonly MaterialPropertyBlock captainDamageBlock = new MaterialPropertyBlock();
        private float targetX;
        private float routeCenterX;
        private bool dragging;
        private Vector2 lastPointer;
        private int builtSoldierCount = -1;
        private int suitIndex;
        private float muzzleFlashTimer;

        public CaptainSquad(Transform ownerTransform)
        {
            owner = ownerTransform;
        }

        public float X
        {
            get { return root == null ? 0f : root.transform.position.x; }
        }

        public float RelativeX
        {
            get { return root == null ? 0f : root.transform.position.x - routeCenterX; }
        }

        public Vector3 CaptainPosition
        {
            get { return captain == null ? new Vector3(0f, 0.8f, GameTheme.CaptainZ) : captain.position; }
        }

        public int VisibleSoldierCount
        {
            get { return soldiers.Count; }
        }

        public int ShooterCount
        {
            get { return (captainMuzzle == null ? 0 : 1) + muzzles.Count; }
        }

        public void Build(RunModel model, int selectedSuit)
        {
            Clear();
            suitIndex = selectedSuit;
            root = new GameObject("CaptainSquad");
            root.transform.SetParent(owner, false);
            root.transform.position = new Vector3(0f, 0f, 0f);
            targetX = 0f;
            routeCenterX = 0f;
            captain = BuildUnit("Captain", root.transform, new Vector3(0f, 0f, GameTheme.CaptainZ + 0.55f), 1f, GameTheme.SuitColor(suitIndex), true);
            captainBaseScale = captain.localScale;
            captainMuzzle = captain.Find("Muzzle");
            RegisterMuzzleFlash(captainMuzzle);
            Refresh(model.SoldierCount);
        }

        public void Clear()
        {
            soldiers.Clear();
            soldierBaseScales.Clear();
            muzzles.Clear();
            muzzleFlashes.Clear();
            muzzleFlashBaseScales.Clear();
            captain = null;
            captainMuzzle = null;
            captainBaseScale = Vector3.one;
            builtSoldierCount = -1;
            muzzleFlashTimer = 0f;
            if (root != null)
            {
                Object.Destroy(root);
                root = null;
            }
        }

        public void TickInput(float deltaTime, bool blocked, float currentRouteCenterX)
        {
            UpdateMuzzleFlashes(deltaTime);
            if (root == null || blocked)
            {
                dragging = false;
                return;
            }

            if (captain != null)
            {
                captain.localScale = Vector3.Lerp(captain.localScale, captainBaseScale, 1f - Mathf.Exp(-18f * deltaTime));
            }

            for (int i = 0; i < soldiers.Count; i++)
            {
                soldiers[i].localScale = Vector3.Lerp(soldiers[i].localScale, soldierBaseScales[i], 1f - Mathf.Exp(-18f * deltaTime));
            }

            float keyboard = Input.GetAxisRaw("Horizontal");
            if (Mathf.Abs(keyboard) > 0.01f)
            {
                targetX += keyboard * 6.8f * deltaTime;
            }

            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                bool overBottomAction = touch.position.y < Screen.height * 0.16f;
                if (touch.phase == TouchPhase.Began)
                {
                    dragging = !overBottomAction && !IsPointerOverUi(touch.fingerId);
                    lastPointer = touch.position;
                }
                else if (dragging && (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary))
                {
                    MoveByScreenDelta(touch.position - lastPointer);
                    lastPointer = touch.position;
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    dragging = false;
                }
            }
            else if (Input.GetMouseButtonDown(0))
            {
                Vector2 pointer = Input.mousePosition;
                dragging = pointer.y >= Screen.height * 0.16f && !IsPointerOverUi(-1);
                lastPointer = pointer;
            }
            else if (Input.GetMouseButton(0) && dragging)
            {
                Vector2 pointer = Input.mousePosition;
                MoveByScreenDelta(pointer - lastPointer);
                lastPointer = pointer;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                dragging = false;
            }

            targetX = Mathf.Clamp(targetX, -GameTheme.ArenaHalfWidth + 0.35f, GameTheme.ArenaHalfWidth - 0.35f);
            routeCenterX = currentRouteCenterX;
            Vector3 position = root.transform.position;
            position.x = Mathf.Lerp(position.x, routeCenterX + targetX, 1f - Mathf.Exp(-18f * deltaTime));
            root.transform.position = position;
        }

        public void Refresh(int soldierCount)
        {
            soldierCount = Mathf.Clamp(soldierCount, 0, RunModel.VisibleSoldierCap);
            if (root == null || soldierCount == builtSoldierCount)
            {
                return;
            }

            while (soldiers.Count < soldierCount)
            {
                int index = soldiers.Count;
                Transform soldier = BuildUnit("Crew_" + (index + 1), root.transform, FormationPosition(index, soldierCount), 0.72f,
                    Color.Lerp(GameTheme.SuitColor(suitIndex), GameTheme.Text, 0.26f), false);
                soldiers.Add(soldier);
                soldierBaseScales.Add(soldier.localScale);
                muzzles.Add(soldier.Find("Muzzle"));
                RegisterMuzzleFlash(muzzles[muzzles.Count - 1]);
            }

            while (soldiers.Count > soldierCount)
            {
                int last = soldiers.Count - 1;
                Object.Destroy(soldiers[last].gameObject);
                soldiers.RemoveAt(last);
                soldierBaseScales.RemoveAt(last);
                muzzles.RemoveAt(last);
                int flashIndex = 1 + last;
                if (flashIndex < muzzleFlashes.Count)
                {
                    Object.Destroy(muzzleFlashes[flashIndex].gameObject);
                    muzzleFlashes.RemoveAt(flashIndex);
                    muzzleFlashBaseScales.RemoveAt(flashIndex);
                }
            }

            for (int i = 0; i < soldiers.Count; i++)
            {
                soldiers[i].localPosition = FormationPosition(i, soldiers.Count);
                AlignImportedUnitToGround(soldiers[i]);
            }

            builtSoldierCount = soldierCount;
        }

        public void GetMuzzlePositions(List<Vector3> output)
        {
            output.Clear();
            if (captainMuzzle != null)
            {
                output.Add(captainMuzzle.position);
            }

            for (int i = 0; i < muzzles.Count; i++)
            {
                if (muzzles[i] != null)
                {
                    output.Add(muzzles[i].position);
                }
            }
        }

        public void Recoil()
        {
            if (captain != null)
            {
                captain.localScale = new Vector3(captainBaseScale.x, captainBaseScale.y, captainBaseScale.z * 0.93f);
            }

            for (int i = 0; i < soldiers.Count; i++)
            {
                Vector3 baseScale = soldierBaseScales[i];
                soldiers[i].localScale = new Vector3(baseScale.x, baseScale.y, baseScale.z * 0.94f);
            }
        }

        public void PulseMuzzleFlash()
        {
            if (muzzleFlashes.Count == 0)
            {
                return;
            }

            muzzleFlashTimer = 0.09f;
            for (int i = 0; i < muzzleFlashes.Count; i++)
            {
                if (muzzleFlashes[i] != null)
                {
                    muzzleFlashes[i].gameObject.SetActive(true);
                }
            }
        }

        public void SetDamageFlash(bool active)
        {
            if (captain == null)
            {
                return;
            }

            Renderer[] renderers = captain.GetComponentsInChildren<Renderer>();
            captainDamageBlock.Clear();
            if (active)
            {
                captainDamageBlock.SetColor("_Color", Color.Lerp(Color.white, GameTheme.Danger, 0.58f));
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].SetPropertyBlock(active ? captainDamageBlock : null);
            }
        }

        private void MoveByScreenDelta(Vector2 delta)
        {
            if (Screen.width > 0)
            {
                targetX += delta.x / Screen.width * GameTheme.ArenaHalfWidth * 2.5f;
            }
        }

        private static bool IsPointerOverUi(int pointerId)
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            return pointerId >= 0 ? EventSystem.current.IsPointerOverGameObject(pointerId) : EventSystem.current.IsPointerOverGameObject();
        }

        private static Vector3 FormationPosition(int index, int totalSoldiers)
        {
            int row = index / 4;
            int inRow = index % 4;
            int countInRow = Mathf.Min(4, totalSoldiers - row * 4);
            float spacing = countInRow <= 2 ? 1.35f : countInRow == 3 ? 1.05f : 0.82f;
            float x = (inRow - (countInRow - 1) * 0.5f) * spacing;
            float z = GameTheme.CaptainZ + 1.65f + row * 0.88f;
            if (index >= 8)
            {
                x += 0.41f;
            }

            return new Vector3(x, 0f, z);
        }

        private static Transform BuildUnit(string name, Transform parent, Vector3 localPosition, float scale, Color suitColor, bool captainUnit)
        {
            if (captainUnit)
            {
                GameObject captainPrefab = Resources.Load<GameObject>(CaptainPrefabResourcePath);
                if (captainPrefab == null)
                {
                    captainPrefab = Resources.Load<GameObject>(CaptainSourceResourcePath);
                }

                if (captainPrefab != null)
                {
                    return BuildImportedCaptain(name, parent, localPosition, scale, captainPrefab);
                }
            }

            if (!captainUnit)
            {
                GameObject soldierPrefab = Resources.Load<GameObject>("Enemies/CrewSoldier");
                if (soldierPrefab != null)
                {
                    return BuildImportedSoldier(name, parent, localPosition, scale, soldierPrefab);
                }
            }

            GameObject unit = new GameObject(name);
            unit.transform.SetParent(parent, false);
            unit.transform.localPosition = localPosition;

            PrimitiveFactory.Capsule("Body", unit.transform, unit.transform.position + Vector3.up * (0.72f * scale),
                new Vector3(0.38f, 0.48f, 0.38f) * scale, suitColor);
            PrimitiveFactory.Cube("Visor", unit.transform, unit.transform.position + new Vector3(0f, 1.02f * scale, 0.25f * scale),
                new Vector3(0.48f, 0.24f, 0.18f) * scale, GameTheme.Surface);
            PrimitiveFactory.Cube("Pack", unit.transform, unit.transform.position + new Vector3(0f, 0.72f * scale, -0.25f * scale),
                new Vector3(0.44f, 0.50f, 0.18f) * scale, GameTheme.Rule);
            PrimitiveFactory.Cube("Weapon", unit.transform, unit.transform.position + new Vector3(0.34f * scale, 0.65f * scale, 0.42f * scale),
                new Vector3(0.18f, 0.16f, captainUnit ? 0.72f : 0.58f) * scale, GameTheme.Weapon);

            GameObject muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(unit.transform, false);
            muzzle.transform.localPosition = new Vector3(0.34f * scale, 0.65f * scale, (captainUnit ? 0.80f : 0.66f) * scale);
            BuildMuzzleFlash(muzzle.transform, scale);
            return unit.transform;
        }

        private static Transform BuildImportedCaptain(string name, Transform parent, Vector3 localPosition, float scale, GameObject prefab)
        {
            GameObject unit = Object.Instantiate(prefab, parent);
            unit.name = name;
            unit.transform.localPosition = localPosition;
            unit.transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * unit.transform.localRotation;
            unit.transform.localScale *= scale * ImportedCaptainVisualScale;

            Transform sourceMuzzle = FindDescendant(unit.transform, "MuzzlePoint");
            GameObject muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(unit.transform, false);
            if (sourceMuzzle != null)
            {
                muzzle.transform.position = sourceMuzzle.position;
                muzzle.transform.rotation = sourceMuzzle.rotation;
            }
            else
            {
                muzzle.transform.localPosition = new Vector3(0.34f * scale, 0.65f * scale, 0.80f * scale);
            }

            BuildMuzzleFlash(muzzle.transform, scale);

            return unit.transform;
        }

        private static Transform BuildImportedSoldier(string name, Transform parent, Vector3 localPosition, float scale, GameObject prefab)
        {
            GameObject unit = Object.Instantiate(prefab, parent);
            unit.name = name;
            unit.transform.localPosition = localPosition;
            unit.transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * unit.transform.localRotation;
            // CrewSoldier was authored at a smaller FBX export scale than the
            // Captain. Restore readable mobile height before grounding it.
            unit.transform.localScale *= scale * ImportedSoldierVisualScale;

            Transform sourceMuzzle = FindDescendant(unit.transform, "MuzzlePoint");
            GameObject muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(unit.transform, false);
            if (sourceMuzzle != null)
            {
                muzzle.transform.position = sourceMuzzle.position;
                muzzle.transform.rotation = sourceMuzzle.rotation;
            }
            else
            {
                muzzle.transform.localPosition = new Vector3(0.34f * scale, 0.65f * scale, 0.80f * scale);
            }

            BuildMuzzleFlash(muzzle.transform, scale);

            return unit.transform;
        }

        private void RegisterMuzzleFlash(Transform muzzle)
        {
            if (muzzle == null)
            {
                return;
            }

            Transform flash = muzzle.Find("MuzzleFlash");
            if (flash == null)
            {
                return;
            }

            flash.gameObject.SetActive(false);
            muzzleFlashes.Add(flash);
            muzzleFlashBaseScales.Add(flash.localScale);
        }

        private void UpdateMuzzleFlashes(float deltaTime)
        {
            if (muzzleFlashTimer <= 0f)
            {
                return;
            }

            muzzleFlashTimer = Mathf.Max(0f, muzzleFlashTimer - deltaTime);
            float normalized = Mathf.Clamp01(muzzleFlashTimer / 0.09f);
            float scale = 0.70f + normalized * 0.45f;
            for (int i = 0; i < muzzleFlashes.Count; i++)
            {
                Transform flash = muzzleFlashes[i];
                if (flash == null)
                {
                    continue;
                }

                flash.localScale = muzzleFlashBaseScales[i] * scale;
                flash.gameObject.SetActive(muzzleFlashTimer > 0f);
            }
        }

        private static void BuildMuzzleFlash(Transform muzzle, float scale)
        {
            Vector3 desiredWorldScale = new Vector3(0.08f, 0.001f, 0.14f) * Mathf.Max(0.72f, scale);
            GameObject flash = PrimitiveFactory.Sphere("MuzzleFlash", muzzle, Vector3.zero,
                Vector3.one, GameTheme.SignalBright);
            flash.transform.localPosition = Vector3.zero;
            flash.transform.rotation = Quaternion.identity;
            Vector3 parentScale = muzzle.lossyScale;
            flash.transform.localScale = new Vector3(
                desiredWorldScale.x / Mathf.Max(0.001f, Mathf.Abs(parentScale.x)),
                desiredWorldScale.y / Mathf.Max(0.001f, Mathf.Abs(parentScale.y)),
                desiredWorldScale.z / Mathf.Max(0.001f, Mathf.Abs(parentScale.z)));
            flash.SetActive(false);
        }

        private static void AlignImportedUnitToGround(Transform unit)
        {
            Renderer[] renderers = unit.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            if (bounds.min.y < 0.02f)
            {
                unit.position += Vector3.up * (0.02f - bounds.min.y);
            }
        }

        private static Transform FindDescendant(Transform root, string targetName)
        {
            if (root.name == targetName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform match = FindDescendant(root.GetChild(i), targetName);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }
    }
}
