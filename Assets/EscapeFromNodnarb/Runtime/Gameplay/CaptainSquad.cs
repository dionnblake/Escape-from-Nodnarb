using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EscapeFromNodnarb
{
    public sealed class CaptainSquad
    {
        private sealed class SoldierMotionParts
        {
            public Transform LeftLeg;
            public Transform RightLeg;
            public Transform LeftArm;
            public Transform RightArm;
            public Quaternion LeftLegBaseRotation;
            public Quaternion RightLegBaseRotation;
            public Quaternion LeftArmBaseRotation;
            public Quaternion RightArmBaseRotation;

            public bool HasParts
            {
                get { return LeftLeg != null || RightLeg != null || LeftArm != null || RightArm != null; }
            }
        }

        private const string CaptainPrefabResourcePath = "Captain/CaptainVisual";
        private const string CaptainSourceResourcePath = "Captain/Captain_Unity";
        // Keep the captain nearest the incoming horde and the crew trailing
        // toward the camera while preserving a readable portrait silhouette.
        private const float ImportedCaptainVisualScale = 0.82f;
        private const float ImportedSoldierVisualScale = 1.12f;
        private const float CaptainLocalZOffset = 1.35f;
        private const float CrewFirstRowZOffset = 0.80f;
        private const float CrewRowZSpacing = 0.82f;
        private const float MotionLeanDegreesPerSpeed = 1.25f;
        private const float MotionMaxLeanDegrees = 8f;
        private const float MotionBobAmplitude = 0.035f;
        private const float MotionBobPhasePerSpeed = 4.2f;
        private const float MotionStartSpeed = 0.08f;
        private const float MotionFullSpeed = 0.65f;
        private const float MotionBlendSpeed = 16f;
        private const float MotionPhaseOffset = 0.52f;
        private readonly Transform owner;
        private readonly List<Transform> soldiers = new List<Transform>();
        private readonly List<Vector3> soldierBaseScales = new List<Vector3>();
        private readonly List<Vector3> soldierBasePositions = new List<Vector3>();
        private readonly List<Quaternion> soldierBaseRotations = new List<Quaternion>();
        private readonly List<SoldierMotionParts> soldierMotionParts = new List<SoldierMotionParts>();
        private readonly List<Transform> muzzles = new List<Transform>();
        private readonly List<Transform> muzzleFlashes = new List<Transform>();
        private readonly List<Vector3> muzzleFlashBaseScales = new List<Vector3>();
        private GameObject root;
        private Transform captain;
        private Transform captainMuzzle;
        private Vector3 captainBaseScale = Vector3.one;
        private Vector3 captainBasePosition;
        private Quaternion captainBaseRotation = Quaternion.identity;
        private readonly MaterialPropertyBlock captainDamageBlock = new MaterialPropertyBlock();
        private readonly MaterialPropertyBlock muzzleAccessibilityBlock = new MaterialPropertyBlock();
        private float targetX;
        private float routeCenterX;
        private bool dragging;
        private Vector2 lastPointer;
        private int activeTouchId = -1;
        private int builtSoldierCount = -1;
        private int suitIndex;
        private float muzzleFlashTimer;
        private float lastRootX;
        private float motionVelocityX;
        private float motionPhase;

        public bool AccessibilityVisualApplied { get; private set; }

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

        public float TargetRelativeX
        {
            get { return targetX; }
        }

        public Vector3 CaptainPosition
        {
            get { return captain == null ? new Vector3(0f, 0.8f, GameTheme.CaptainZ + CaptainLocalZOffset) : captain.position; }
        }

        public int VisibleSoldierCount
        {
            get { return soldiers.Count; }
        }

        public int ShooterCount
        {
            get { return (captainMuzzle == null ? 0 : 1) + muzzles.Count; }
        }

        public bool IsDragging
        {
            get { return dragging; }
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
            captain = BuildUnit("Captain", root.transform, new Vector3(0f, 0f, GameTheme.CaptainZ + CaptainLocalZOffset), 1f, GameTheme.SuitColor(suitIndex), true);
            captainBaseScale = captain.localScale;
            captainBasePosition = captain.localPosition;
            captainBaseRotation = captain.localRotation;
            captainMuzzle = captain.Find("Muzzle");
            RegisterMuzzleFlash(captainMuzzle);
            lastRootX = root.transform.position.x;
            motionVelocityX = 0f;
            motionPhase = 0f;
            Refresh(model.SoldierCount);
            ApplyAccessibilitySettings();
        }

        public void RestoreRelativePosition(float relativeX, float targetRelativeX, float currentRouteCenterX)
        {
            if (root == null)
            {
                return;
            }

            float maxOffset = GameTheme.ArenaHalfWidth - 0.35f;
            float safeRelativeX = float.IsNaN(relativeX) || float.IsInfinity(relativeX)
                ? 0f
                : Mathf.Clamp(relativeX, -maxOffset, maxOffset);
            float safeTargetX = float.IsNaN(targetRelativeX) || float.IsInfinity(targetRelativeX)
                ? 0f
                : Mathf.Clamp(targetRelativeX, -maxOffset, maxOffset);
            routeCenterX = currentRouteCenterX;
            targetX = safeTargetX;
            Vector3 position = root.transform.position;
            position.x = routeCenterX + safeRelativeX;
            root.transform.position = position;
            lastRootX = position.x;
            motionVelocityX = 0f;
            dragging = false;
            activeTouchId = -1;
        }

        public void Clear()
        {
            soldiers.Clear();
            soldierBaseScales.Clear();
            soldierBasePositions.Clear();
            soldierBaseRotations.Clear();
            soldierMotionParts.Clear();
            muzzles.Clear();
            muzzleFlashes.Clear();
            muzzleFlashBaseScales.Clear();
            captain = null;
            captainMuzzle = null;
            captainBaseScale = Vector3.one;
            captainBasePosition = Vector3.zero;
            captainBaseRotation = Quaternion.identity;
            builtSoldierCount = -1;
            muzzleFlashTimer = 0f;
            lastRootX = 0f;
            motionVelocityX = 0f;
            motionPhase = 0f;
            activeTouchId = -1;
            if (root != null)
            {
                Object.Destroy(root);
                root = null;
            }
        }

        public void TickInput(float deltaTime, bool blocked, float currentRouteCenterX)
        {
            UpdateMuzzleFlashes(deltaTime);
            if (root == null)
            {
                dragging = false;
                return;
            }

            if (blocked)
            {
                dragging = false;
                lastRootX = root.transform.position.x;
                UpdateUnitMotion(deltaTime, 0f);
                return;
            }

            float keyboard = Input.GetAxisRaw("Horizontal");
            if (Mathf.Abs(keyboard) > 0.01f)
            {
                targetX += keyboard * 6.8f * deltaTime;
            }

            if (Input.touchCount > 0)
            {
                Touch touch;
                if (TryGetActiveTouch(out touch))
                {
                    if (touch.phase == TouchPhase.Began)
                    {
                        dragging = NodnarbInputPolicy.CanBeginWorldGesture(
                            touch.position,
                            Screen.safeArea,
                            Screen.height,
                            IsPointerOverUi(touch.fingerId));
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
                        activeTouchId = -1;
                    }
                }
            }
            else if (Input.GetMouseButtonDown(0))
            {
                Vector2 pointer = Input.mousePosition;
                dragging = NodnarbInputPolicy.CanBeginWorldGesture(
                    pointer,
                    Screen.safeArea,
                    Screen.height,
                    IsPointerOverUi(-1));
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

            float safeDeltaTime = Mathf.Max(0.0001f, deltaTime);
            float velocityX = (position.x - lastRootX) / safeDeltaTime;
            lastRootX = position.x;
            UpdateUnitMotion(deltaTime, velocityX);
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
                soldierBasePositions.Add(soldier.localPosition);
                soldierBaseRotations.Add(soldier.localRotation);
                soldierMotionParts.Add(FindSoldierMotionParts(soldier));
                muzzles.Add(soldier.Find("Muzzle"));
                RegisterMuzzleFlash(muzzles[muzzles.Count - 1]);
            }

            while (soldiers.Count > soldierCount)
            {
                int last = soldiers.Count - 1;
                Object.Destroy(soldiers[last].gameObject);
                soldiers.RemoveAt(last);
                soldierBaseScales.RemoveAt(last);
                soldierBasePositions.RemoveAt(last);
                soldierBaseRotations.RemoveAt(last);
                soldierMotionParts.RemoveAt(last);
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
                soldierBasePositions[i] = soldiers[i].localPosition;
                soldierBaseRotations[i] = soldiers[i].localRotation;
            }

            builtSoldierCount = soldierCount;
            ApplyAccessibilitySettings();
        }

        public void ApplyAccessibilitySettings()
        {
            for (int i = 0; i < muzzleFlashes.Count; i++)
            {
                Transform flash = muzzleFlashes[i];
                if (flash == null)
                {
                    continue;
                }

                Renderer[] renderers = flash.GetComponentsInChildren<Renderer>(true);
                for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    if (NodnarbSettings.HighContrastEnabled)
                    {
                        muzzleAccessibilityBlock.Clear();
                        muzzleAccessibilityBlock.SetColor("_Color", GameTheme.AccessibleSignalBright);
                        renderers[rendererIndex].SetPropertyBlock(muzzleAccessibilityBlock);
                    }
                    else
                    {
                        renderers[rendererIndex].SetPropertyBlock(null);
                    }
                }
            }

            AccessibilityVisualApplied = true;
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
            if (NodnarbSettings.ReducedMotionEnabled)
            {
                return;
            }

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
                captainDamageBlock.SetColor("_Color", Color.Lerp(Color.white, GameTheme.AccessibleDanger, 0.58f));
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].SetPropertyBlock(active ? captainDamageBlock : null);
            }
        }

        private void MoveByScreenDelta(Vector2 delta)
        {
            Rect safeArea = Screen.safeArea;
            float width = safeArea.width > 0f ? safeArea.width : Screen.width;
            if (width > 0f)
            {
                targetX += delta.x / width * GameTheme.ArenaHalfWidth * 2.5f;
            }
        }

        private bool TryGetActiveTouch(out Touch activeTouch)
        {
            activeTouch = default(Touch);
            if (activeTouchId >= 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch touch = Input.GetTouch(i);
                    if (touch.fingerId == activeTouchId)
                    {
                        activeTouch = touch;
                        return true;
                    }
                }

                activeTouchId = -1;
            }

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began
                    && NodnarbInputPolicy.CanBeginWorldGesture(
                        touch.position,
                        Screen.safeArea,
                        Screen.height,
                        IsPointerOverUi(touch.fingerId)))
                {
                    activeTouchId = touch.fingerId;
                    activeTouch = touch;
                    return true;
                }
            }

            return false;
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
            float spacing = countInRow <= 2 ? 1.08f : countInRow == 3 ? 0.92f : 0.76f;
            float x = (inRow - (countInRow - 1) * 0.5f) * spacing;
            float z = GameTheme.CaptainZ + CaptainLocalZOffset - CrewFirstRowZOffset - row * CrewRowZSpacing;
            if (index >= 8)
            {
                x += 0.41f;
            }

            return new Vector3(x, 0f, z);
        }

        private void UpdateUnitMotion(float deltaTime, float velocityX)
        {
            if (captain == null)
            {
                return;
            }

            if (NodnarbSettings.ReducedMotionEnabled)
            {
                motionVelocityX = 0f;
                captain.localPosition = captainBasePosition;
                captain.localRotation = captainBaseRotation;
                captain.localScale = captainBaseScale;
                for (int i = 0; i < soldiers.Count; i++)
                {
                    soldiers[i].localPosition = soldierBasePositions[i];
                    soldiers[i].localRotation = soldierBaseRotations[i];
                    soldiers[i].localScale = soldierBaseScales[i];
                    RestoreSoldierMotionParts(soldierMotionParts[i]);
                }

                return;
            }

            float safeDeltaTime = Mathf.Max(0.0001f, deltaTime);
            float blend = 1f - Mathf.Exp(-MotionBlendSpeed * safeDeltaTime);
            motionVelocityX = Mathf.Lerp(motionVelocityX, velocityX, blend);
            float speed = Mathf.Abs(motionVelocityX);
            float movement01 = Mathf.InverseLerp(MotionStartSpeed, MotionFullSpeed, speed);
            if (speed > MotionStartSpeed)
            {
                motionPhase = Mathf.Repeat(motionPhase + speed * MotionBobPhasePerSpeed * safeDeltaTime, Mathf.PI * 2f);
            }

            float leanDegrees = Mathf.Clamp(-motionVelocityX * MotionLeanDegreesPerSpeed,
                -MotionMaxLeanDegrees, MotionMaxLeanDegrees);
            ApplyUnitMotion(captain, captainBasePosition, captainBaseRotation, captainBaseScale,
                motionPhase, movement01, leanDegrees, blend);

            for (int i = 0; i < soldiers.Count; i++)
            {
                ApplyUnitMotion(soldiers[i], soldierBasePositions[i], soldierBaseRotations[i], soldierBaseScales[i],
                    motionPhase + (i + 1) * MotionPhaseOffset, movement01, leanDegrees, blend);
                ApplySoldierLimbMotion(soldierMotionParts[i], motionPhase + (i + 1) * MotionPhaseOffset,
                    movement01, blend);
            }
        }

        private static SoldierMotionParts FindSoldierMotionParts(Transform soldier)
        {
            SoldierMotionParts parts = new SoldierMotionParts
            {
                LeftLeg = FindDescendant(soldier, "Thigh"),
                RightLeg = FindDescendant(soldier, "Thigh.001"),
                LeftArm = FindDescendant(soldier, "UpperArm"),
                RightArm = FindDescendant(soldier, "UpperArm.001")
            };

            if (parts.LeftLeg != null) parts.LeftLegBaseRotation = parts.LeftLeg.localRotation;
            if (parts.RightLeg != null) parts.RightLegBaseRotation = parts.RightLeg.localRotation;
            if (parts.LeftArm != null) parts.LeftArmBaseRotation = parts.LeftArm.localRotation;
            if (parts.RightArm != null) parts.RightArmBaseRotation = parts.RightArm.localRotation;
            return parts;
        }

        private static void RestoreSoldierMotionParts(SoldierMotionParts parts)
        {
            if (parts.LeftLeg != null) parts.LeftLeg.localRotation = parts.LeftLegBaseRotation;
            if (parts.RightLeg != null) parts.RightLeg.localRotation = parts.RightLegBaseRotation;
            if (parts.LeftArm != null) parts.LeftArm.localRotation = parts.LeftArmBaseRotation;
            if (parts.RightArm != null) parts.RightArm.localRotation = parts.RightArmBaseRotation;
        }

        private static void ApplySoldierLimbMotion(SoldierMotionParts parts, float phase, float movement01, float blend)
        {
            if (parts == null || !parts.HasParts)
            {
                return;
            }

            float stride = Mathf.Sin(phase) * 11f * movement01;
            float counterStride = Mathf.Sin(phase + Mathf.PI) * 11f * movement01;
            float armSwing = Mathf.Sin(phase + Mathf.PI) * 7f * movement01;
            float counterArmSwing = Mathf.Sin(phase) * 7f * movement01;
            ApplyPartRotation(parts.LeftLeg, parts.LeftLegBaseRotation, stride, blend);
            ApplyPartRotation(parts.RightLeg, parts.RightLegBaseRotation, counterStride, blend);
            ApplyPartRotation(parts.LeftArm, parts.LeftArmBaseRotation, armSwing, blend);
            ApplyPartRotation(parts.RightArm, parts.RightArmBaseRotation, counterArmSwing, blend);
        }

        private static void ApplyPartRotation(Transform part, Quaternion baseRotation, float angle, float blend)
        {
            if (part == null)
            {
                return;
            }

            Quaternion target = baseRotation * Quaternion.Euler(angle, 0f, 0f);
            part.localRotation = Quaternion.Slerp(part.localRotation, target, blend);
        }

        private static void ApplyUnitMotion(Transform unit, Vector3 basePosition, Quaternion baseRotation,
            Vector3 baseScale, float phase, float movement01, float leanDegrees, float blend)
        {
            if (unit == null)
            {
                return;
            }

            float bob = Mathf.Sin(phase) * MotionBobAmplitude * movement01;
            Vector3 targetPosition = basePosition + Vector3.up * bob;
            unit.localPosition = Vector3.Lerp(unit.localPosition, targetPosition, blend);

            Quaternion targetRotation = baseRotation * Quaternion.Euler(0f, 0f, leanDegrees);
            unit.localRotation = Quaternion.Slerp(unit.localRotation, targetRotation, blend);

            float weightShift = Mathf.Sin(phase + MotionPhaseOffset);
            Vector3 targetScale = baseScale;
            targetScale.x *= 1f + weightShift * 0.012f * movement01;
            targetScale.y *= 1f + Mathf.Abs(weightShift) * 0.006f * movement01;
            targetScale.z *= 1f - Mathf.Abs(weightShift) * 0.010f * movement01;
            unit.localScale = Vector3.Lerp(unit.localScale, targetScale, blend);
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
            float scale = NodnarbSettings.ReducedMotionEnabled ? 1f : 0.70f + normalized * 0.45f;
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
