using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace RickMorty
{
    /// <summary>
    /// Fabrica de objetos: crea cualquier item del JSON con fisica, agarre XR, etiqueta,
    /// sonido de impacto y su comportamiento especial. Usa el modelo del ItemVisualRegistry
    /// si existe; si no, arma un placeholder con primitivas y colores del diseno.
    /// </summary>
    public static class ItemFactory
    {
        public static MixableItem Spawn(string id, Vector3 position, Quaternion rotation)
        {
            var go = Build(id, position, rotation, true);
            return go != null ? go.GetComponent<MixableItem>() : null;
        }

        public static GameObject Build(string id, Vector3 position, Quaternion rotation, bool addBehaviour)
        {
            var def = GameDatabase.GetItem(id);
            if (def == null)
            {
                Debug.LogError("[ItemFactory] Item desconocido: " + id);
                return null;
            }

            var go = new GameObject(def.name);
            go.transform.SetPositionAndRotation(position, rotation);

            var visual = new GameObject("Visual").transform;
            visual.SetParent(go.transform, false);
            Transform grip = null;

            var entry = ItemVisualRegistry.Get() != null ? ItemVisualRegistry.Get().Find(id) : null;
            bool hasModelColliders = false;
            if (entry != null)
            {
                var model = Object.Instantiate(entry.prefab, visual);
                model.transform.localPosition = entry.positionOffset;
                model.transform.localRotation = Quaternion.Euler(entry.rotationOffset);
                model.transform.localScale = Vector3.one * entry.scale;
                hasModelColliders = !entry.generateBoxCollider && model.GetComponentInChildren<Collider>() != null;
                DecorateModel(model.transform, def);
            }
            else
            {
                grip = BuildPlaceholder(visual, def);
            }

            BoxCollider box = null;
            if (!hasModelColliders) box = FitBoxCollider(go, visual);
            if (entry != null && box != null) grip = MakeGrip(go.transform, def, box);

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = Mathf.Max(0.05f, def.mass);
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.angularDamping = 0.3f;

            var grab = go.AddComponent<XRGrabInteractable>();
            grab.throwOnDetach = true;
            grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
            if (grip != null)
            {
                grab.useDynamicAttach = false;
                grab.attachTransform = grip;
            }
            else grab.useDynamicAttach = true;

            var item = go.AddComponent<MixableItem>();
            item.Init(id);
            go.AddComponent<ItemLabel>();
            var impact = go.AddComponent<ImpactSound>();
            impact.kind = ImpactKindFor(def);

            if (addBehaviour) AddBehaviour(go, def, grab);
            return go;
        }

        public static ImpactSound.Kind ImpactKindFor(ItemDef def)
        {
            if (def.IsBase || def.shape == "bottle" || def.shape == "jar") return ImpactSound.Kind.Glass;
            if (def.category == "arma" || def.category == "final") return ImpactSound.Kind.Metal;
            if (def.category == "criatura" || def.category == "comida") return ImpactSound.Kind.Flesh;
            return ImpactSound.Kind.Wood;
        }

        static void AddBehaviour(GameObject go, ItemDef def, XRGrabInteractable grab)
        {
            switch (def.behaviour)
            {
                case "laser": go.AddComponent<LaserGun>(); break;
                case "scar": go.AddComponent<ScarRifle>(); break;
                case "melee": go.AddComponent<MeleeWeapon>(); break;
                case "melee_heavy":
                    var mw = go.AddComponent<MeleeWeapon>();
                    mw.burnMarks = true;
                    grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
                    grab.velocityScale = 0.25f;
                    grab.angularVelocityScale = 0.25f;
                    break;
                case "lightsaber": go.AddComponent<Lightsaber>(); break;
                case "knife": go.AddComponent<ThrowingKnife>().canSpin = def.id == "cuchillo_csgo"; break;
                case "cardboard_gun": go.AddComponent<CardboardPortalGun>(); break;
                case "portal_gun": go.AddComponent<RealPortalGun>(); break;
                case "meeseeks_box": go.AddComponent<MeeseeksBox>().contraband = false; break;
                case "contraband_box": go.AddComponent<MeeseeksBox>().contraband = true; break;
                case "gauntlet": go.AddComponent<InfinityGauntlet>(); break;
                case "minecraft": go.AddComponent<FloatingBlock>().spin = false; break;
                case "spinner": go.AddComponent<FloatingBlock>().spin = true; break;
                case "burnable": go.AddComponent<Burnable>(); break;
                case "talker": go.AddComponent<Talker>(); break;
                case "shield": go.AddComponent<Shield>(); break;
                case "helmet_snowball": go.AddComponent<SnowballHelmet>(); break;
                case "food_drunk": go.AddComponent<Consumable>().effect = Consumable.Effect.Drunk; break;
                case "food_vhs": go.AddComponent<Consumable>().effect = Consumable.Effect.VHS; break;
                case "food_teleport": go.AddComponent<Consumable>().effect = Consumable.Effect.Teleport; break;
                case "food_eyeholes": go.AddComponent<Consumable>().effect = Consumable.Effect.Eyeholes; break;
            }

            if (def.category == "criatura" && def.behaviour != "meeseeks_box" && def.behaviour != "contraband_box" && def.id != "clon_rick")
            {
                var cr = go.AddComponent<Creature>();
                ConfigureCreature(cr, def);
                switch (def.behaviour)
                {
                    case "creature_pet": go.AddComponent<PetHinter>(); break;
                    case "creature_throwable": go.AddComponent<StickyThrowable>(); break;
                    case "creature_wander": go.AddComponent<CreatureMover>().speed = 0.35f; break;
                    case "creature_steve": go.AddComponent<FakeUncleSteve>(); break;
                    case "creature_caleno": go.AddComponent<RickCaleno>(); break;
                }
            }
        }

        public static void ConfigureCreature(Creature cr, ItemDef def)
        {
            cr.displayName = def.name;
            cr.splatColor = def.Color;
            switch (def.id)
            {
                case "tio_steve": cr.splatColor = new Color(0.4f, 1f, 0.3f); cr.killLineId = "kill_steve"; break;
                case "sr_pantalones": cr.killLineId = "kill_pantalones"; cr.moralPenalty = 150; cr.splatColor = new Color(0.8f, 0.1f, 0.1f); break;
                case "meeseeks_kirkland": cr.killLineId = "kill_meeseeks"; cr.splatColor = new Color(0.3f, 0.6f, 1f); break;
                case "mini_cronenberg": cr.splatColor = new Color(0.62f, 0.12f, 0.45f); break;   // liquido negro-morado (en el asfalto el negro puro no se notaba)
                case "rick_caleno": cr.killLineId = "kill_caleno"; break;
            }
        }

        // ------------------------------------------------------------------
        //  Placeholders con primitivas
        // ------------------------------------------------------------------

        static GameObject Prim(PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Material mat, Vector3 euler = default)
        {
            var p = GameObject.CreatePrimitive(type);
            var col = p.GetComponent<Collider>();
            if (Application.isPlaying) Object.Destroy(col); else Object.DestroyImmediate(col);
            p.transform.SetParent(parent, false);
            p.transform.localPosition = pos;
            p.transform.localRotation = Quaternion.Euler(euler);
            p.transform.localScale = scale;
            p.GetComponent<Renderer>().sharedMaterial = mat;
            return p;
        }

        /// <summary>Construye el placeholder. Devuelve el punto de agarre (para armas) o null.</summary>
        public static Transform BuildPlaceholder(Transform root, ItemDef def)
        {
            var c = def.Color;
            float s = def.size;
            bool glow = def.id == "kalaxianos" || def.id == "mega_semilla" || def.id == "acido" || def.id == "marihuana" || def.id == "semen"
                        || def.id == "clon_rick" || def.id == "token_fortnite" || def.id == "meeseeks";
            var body = def.id == "materia_oscura" ? MatLib.Unlit(new Color(0.01f, 0.01f, 0.01f)) : MatLib.Lit(c, glow ? 1.2f : 0f);
            var glass = MatLib.Transparent(new Color(0.85f, 0.95f, 1f, 0.22f));
            var metal = MatLib.Lit(new Color(0.55f, 0.57f, 0.6f), 0f, 0.7f, 0.8f);
            var dark = MatLib.Lit(new Color(0.08f, 0.08f, 0.08f));
            Transform grip = null;

            switch (def.shape)
            {
                case "flask":
                    Prim(PrimitiveType.Sphere, root, new Vector3(0, -0.03f, 0), new Vector3(0.1f, 0.1f, 0.1f), glass);
                    Prim(PrimitiveType.Sphere, root, new Vector3(0, -0.04f, 0), new Vector3(0.085f, 0.07f, 0.085f), body);
                    Prim(PrimitiveType.Cylinder, root, new Vector3(0, 0.04f, 0), new Vector3(0.035f, 0.045f, 0.035f), glass);
                    Prim(PrimitiveType.Cylinder, root, new Vector3(0, 0.09f, 0), new Vector3(0.03f, 0.01f, 0.03f), MatLib.Lit(new Color(0.6f, 0.45f, 0.3f)));
                    break;
                case "jar":
                    Prim(PrimitiveType.Cylinder, root, Vector3.zero, new Vector3(0.09f, 0.06f, 0.09f), glass);
                    Prim(PrimitiveType.Cylinder, root, new Vector3(0, -0.012f, 0), new Vector3(0.078f, 0.045f, 0.078f), body);
                    Prim(PrimitiveType.Cylinder, root, new Vector3(0, 0.065f, 0), new Vector3(0.095f, 0.008f, 0.095f), metal);
                    if (s > 0.11f) root.localScale = Vector3.one * (s / 0.1f);
                    break;
                case "bottle":
                    Prim(PrimitiveType.Cylinder, root, Vector3.zero, new Vector3(0.07f, 0.08f, 0.07f), MatLib.Transparent(new Color(0.9f, 0.97f, 1f, 0.35f)));
                    Prim(PrimitiveType.Cylinder, root, new Vector3(0, -0.01f, 0), new Vector3(0.062f, 0.065f, 0.062f), MatLib.Transparent(new Color(1f, 1f, 1f, 0.5f)));
                    Prim(PrimitiveType.Cylinder, root, new Vector3(0, 0.1f, 0), new Vector3(0.028f, 0.03f, 0.028f), MatLib.Transparent(new Color(0.9f, 0.97f, 1f, 0.35f)));
                    Prim(PrimitiveType.Cylinder, root, new Vector3(0, 0.135f, 0), new Vector3(0.03f, 0.008f, 0.03f), MatLib.Lit(new Color(0.1f, 0.3f, 0.9f)));
                    Prim(PrimitiveType.Cylinder, root, new Vector3(0, 0.0f, 0), new Vector3(0.072f, 0.025f, 0.072f), MatLib.Lit(new Color(0.85f, 0.15f, 0.15f)));
                    break;
                case "cylinder":
                    Prim(PrimitiveType.Cylinder, root, Vector3.zero, new Vector3(s * 0.7f, s * 0.5f, s * 0.7f), body);
                    Prim(PrimitiveType.Cylinder, root, new Vector3(0, s * 0.52f, 0), new Vector3(s * 0.3f, s * 0.04f, s * 0.3f), MatLib.Lit(Color.white));
                    break;
                case "sphere":
                    Prim(PrimitiveType.Sphere, root, Vector3.zero, Vector3.one * s, body);
                    break;
                case "capsule":
                    Prim(PrimitiveType.Capsule, root, Vector3.zero, new Vector3(s * 0.5f, s * 0.5f, s * 0.5f), body);
                    break;
                case "half":
                    Prim(PrimitiveType.Sphere, root, Vector3.zero, new Vector3(s, s * 0.4f, s * 0.6f), body);
                    break;
                case "coin":
                    Prim(PrimitiveType.Cylinder, root, Vector3.zero, new Vector3(s, 0.012f, s), body, new Vector3(90, 0, 0));
                    break;
                case "pillow":
                    Prim(PrimitiveType.Cube, root, Vector3.zero, new Vector3(s * 0.4f, s, 0.12f), body);
                    Prim(PrimitiveType.Cube, root, new Vector3(0, s * 0.1f, -0.061f), new Vector3(s * 0.3f, s * 0.6f, 0.002f), MatLib.Lit(new Color(0.95f, 0.95f, 1f)));
                    break;
                case "blade":
                {
                    float len = s * 0.75f;
                    grip = new GameObject("Grip").transform;
                    grip.SetParent(root.parent, false);
                    grip.localPosition = new Vector3(0, 0, -s * 0.35f);
                    Prim(PrimitiveType.Cylinder, root, new Vector3(0, 0, -s * 0.38f), new Vector3(0.035f, 0.1f, 0.035f), dark, new Vector3(90, 0, 0));
                    Prim(PrimitiveType.Cube, root, new Vector3(0, 0, -s * 0.26f), new Vector3(0.14f, 0.02f, 0.03f), MatLib.Lit(new Color(0.85f, 0.7f, 0.2f), 0f, 0.6f, 0.8f));
                    var blade = Prim(PrimitiveType.Cube, root, new Vector3(0, 0, -s * 0.25f + len * 0.5f), new Vector3(def.id == "espada_zord" ? 0.12f : 0.04f, 0.01f, len),
                        def.id == "espada_zord" ? MatLib.Lit(c, 0.2f, 0.9f, 0.5f) : MatLib.Lit(c, 0f, 0.95f, 1f));
                    blade.name = "Blade";
                    break;
                }
                case "knife":
                    grip = new GameObject("Grip").transform;
                    grip.SetParent(root.parent, false);
                    grip.localPosition = new Vector3(0, 0, -0.05f);
                    Prim(PrimitiveType.Cube, root, new Vector3(0, 0, -0.05f), new Vector3(0.025f, 0.03f, 0.1f), dark);
                    Prim(PrimitiveType.Cube, root, new Vector3(0, 0, 0.06f), new Vector3(0.008f, 0.03f, 0.12f), MatLib.Lit(c, 0f, 0.8f, 0.6f));
                    break;
                case "hilt":
                    grip = new GameObject("Grip").transform;
                    grip.SetParent(root.parent, false);
                    Prim(PrimitiveType.Cylinder, root, Vector3.zero, new Vector3(0.04f, s * 0.5f, 0.04f), metal, new Vector3(90, 0, 0));
                    Prim(PrimitiveType.Cube, root, new Vector3(0, 0.022f, 0.03f), new Vector3(0.01f, 0.01f, 0.02f), MatLib.Lit(Color.red));
                    break;
                case "gun":
                {
                    grip = new GameObject("Grip").transform;
                    grip.SetParent(root.parent, false);
                    grip.localPosition = new Vector3(0, -0.06f, -s * 0.15f);
                    grip.localRotation = Quaternion.Euler(-15, 0, 0);
                    var mat = def.id == "pistola_portales" ? MatLib.Lit(new Color(0.95f, 0.95f, 0.95f), 0f, 0.8f) : body;
                    Prim(PrimitiveType.Cube, root, Vector3.zero, new Vector3(0.06f, 0.07f, s * 0.7f), mat);
                    Prim(PrimitiveType.Cylinder, root, new Vector3(0, 0.01f, s * 0.45f), new Vector3(0.035f, s * 0.12f, 0.035f), def.id == "portal_carton" ? body : metal, new Vector3(90, 0, 0));
                    Prim(PrimitiveType.Cube, root, new Vector3(0, -0.065f, -s * 0.15f), new Vector3(0.04f, 0.09f, 0.045f), def.id == "portal_carton" ? body : dark, new Vector3(-15, 0, 0));
                    if (def.id == "pistola_laser")
                        Prim(PrimitiveType.Cylinder, root, new Vector3(0, 0.01f, s * 0.3f), new Vector3(0.05f, 0.006f, 0.05f), MatLib.Lit(new Color(0.2f, 1f, 0.9f), 2f), new Vector3(90, 0, 0));
                    if (def.id == "pistola_portales" || def.id == "portal_carton")
                    {
                        var tubeMat = def.id == "pistola_portales" ? MatLib.Lit(new Color(0.3f, 1f, 0.3f), 3f) : MatLib.Lit(new Color(0.2f, 0.7f, 0.2f));
                        Prim(PrimitiveType.Capsule, root, new Vector3(0, 0.055f, 0), new Vector3(0.035f, s * 0.2f, 0.035f), tubeMat, new Vector3(90, 0, 0)).name = "Tube";
                    }
                    if (def.id == "scar")
                        Prim(PrimitiveType.Cube, root, new Vector3(0, -0.03f, s * 0.15f), new Vector3(0.03f, 0.08f, 0.05f), dark);
                    break;
                }
                default: // cube
                    Prim(PrimitiveType.Cube, root, Vector3.zero, Vector3.one * s, body);
                    if (def.id == "bloque_minecraft")
                        Prim(PrimitiveType.Cube, root, new Vector3(0, s * 0.45f, 0), new Vector3(s * 1.01f, s * 0.12f, s * 1.01f), MatLib.Lit(new Color(0.35f, 0.75f, 0.2f)));
                    break;
            }

            if (def.category == "criatura" && def.shape != "cylinder" && def.shape != "cube" && def.id != "clon_rick")
                AddEyes(root, s);

            return grip;
        }

        public static void AddEyes(Transform root, float s)
        {
            var white = MatLib.Lit(Color.white);
            var black = MatLib.Unlit(Color.black);
            for (int i = -1; i <= 1; i += 2)
            {
                var eye = Prim(PrimitiveType.Sphere, root, new Vector3(i * s * 0.16f, s * 0.18f, s * 0.22f), Vector3.one * s * 0.18f, white);
                Prim(PrimitiveType.Sphere, eye.transform, new Vector3(0, 0, 0.35f), Vector3.one * 0.5f, black);
            }
        }

        /// <summary>
        /// Detalles sobre los modelos reales: liquido de color dentro de los frascos base,
        /// feto de Rick en la bola de clon y accesorios del Rick caleno.
        /// </summary>
        static void DecorateModel(Transform model, ItemDef def)
        {
            if (def.IsBase || def.id == "marihuana" || def.id == "semen")
            {
                if (def.shape == "jar")
                {
                    // el vidrio del frasco importa casi invisible: vidrio propio, tenue pero visible
                    var glass = MatLib.Transparent(new Color(0.85f, 0.95f, 1f, 0.22f));
                    foreach (var r in model.GetComponentsInChildren<Renderer>())
                    {
                        var mats = r.sharedMaterials;
                        for (int i = 0; i < mats.Length; i++)
                            if (mats[i] != null && mats[i].renderQueue >= 2900) mats[i] = glass;
                        r.sharedMaterials = mats;
                    }
                }
                var color = def.Color;
                var liquidMat = def.id == "materia_oscura" ? MatLib.Unlit(new Color(0.01f, 0.01f, 0.01f))
                    : MatLib.Lit(color, def.id == "kalaxianos" || def.id == "acido" || def.id == "mega_semilla" || def.id == "meeseeks" || def.id == "marihuana" || def.id == "semen" ? 1.2f : 0.3f, 0.6f);
                foreach (var mf in model.GetComponentsInChildren<MeshFilter>())
                {
                    if (mf.sharedMesh == null) continue;
                    var b = mf.sharedMesh.bounds;
                    bool jar = def.shape == "jar";
                    float sxz = jar ? 0.86f : 0.8f, sy = jar ? 0.78f : 0.55f;
                    var liq = new GameObject("Liquido");
                    liq.transform.SetParent(mf.transform, false);
                    liq.transform.localScale = new Vector3(sxz, sy, sxz);
                    liq.transform.localPosition = new Vector3(b.center.x * (1 - sxz), b.min.y * (1 - sy) + b.size.y * 0.04f, b.center.z * (1 - sxz));
                    liq.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
                    var mr = liq.AddComponent<MeshRenderer>();
                    var mats = new Material[mf.sharedMesh.subMeshCount];
                    for (int i = 0; i < mats.Length; i++) mats[i] = liquidMat;
                    mr.sharedMaterials = mats;
                }
            }
            else if (def.id == "clon_rick")
            {
                var fetus = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                StripCollider(fetus);
                fetus.name = "FetoRick";
                fetus.transform.SetParent(model, false);
                fetus.transform.localScale = new Vector3(0.035f, 0.045f, 0.035f);
                fetus.GetComponent<Renderer>().sharedMaterial = MatLib.Lit(new Color(1f, 0.82f, 0.75f));
                var hair = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                StripCollider(hair);
                hair.transform.SetParent(fetus.transform, false);
                hair.transform.localPosition = new Vector3(0, 0.35f, -0.1f);
                hair.transform.localScale = new Vector3(0.9f, 0.5f, 0.9f);
                hair.GetComponent<Renderer>().sharedMaterial = MatLib.Lit(new Color(0.6f, 0.82f, 0.95f));
                var fluid = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                StripCollider(fluid);
                fluid.transform.SetParent(model, false);
                fluid.transform.localScale = Vector3.one * 0.088f;
                fluid.GetComponent<Renderer>().sharedMaterial = MatLib.Transparent(new Color(0.3f, 1f, 0.5f, 0.35f));
            }
            else if (def.id == "rick_caleno")
            {
                // Gafas de ciclista oscuras y gorra agachada (coordenadas en metros del modelo de 1.75 m)
                var glasses = GameObject.CreatePrimitive(PrimitiveType.Cube);
                StripCollider(glasses);
                glasses.transform.SetParent(model, false);
                glasses.transform.localPosition = new Vector3(0, 1.5f, 0.12f);
                glasses.transform.localScale = new Vector3(0.24f, 0.06f, 0.05f);
                glasses.GetComponent<Renderer>().sharedMaterial = MatLib.Lit(new Color(0.02f, 0.02f, 0.03f), 0f, 0.9f, 0.8f);
                var cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                StripCollider(cap);
                cap.transform.SetParent(model, false);
                cap.transform.localPosition = new Vector3(0, 1.64f, 0.0f);
                cap.transform.localRotation = Quaternion.Euler(12, 0, 0);
                cap.transform.localScale = new Vector3(0.3f, 0.06f, 0.3f);
                cap.GetComponent<Renderer>().sharedMaterial = MatLib.Lit(new Color(0.8f, 0.05f, 0.1f));
                var visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                StripCollider(visor);
                visor.transform.SetParent(cap.transform, false);
                visor.transform.localPosition = new Vector3(0, -0.2f, 0.6f);
                visor.transform.localScale = new Vector3(0.8f, 0.3f, 0.6f);
                visor.GetComponent<Renderer>().sharedMaterial = MatLib.Lit(new Color(0.8f, 0.05f, 0.1f));
            }
        }

        static void StripCollider(GameObject go)
        {
            var c = go.GetComponent<Collider>();
            if (c == null) return;
            if (Application.isPlaying) Object.Destroy(c); else Object.DestroyImmediate(c);
        }

        /// <summary>Punto de agarre para armas con modelo real: mango atras (-Z) y abajo.</summary>
        static Transform MakeGrip(Transform root, ItemDef def, BoxCollider box)
        {
            var s = box.size; var c = box.center;
            Vector3? local = null;
            Quaternion rot = Quaternion.identity;
            switch (def.shape)
            {
                case "gun":
                    local = new Vector3(c.x, c.y - s.y * 0.25f, c.z - s.z * 0.2f);
                    rot = Quaternion.Euler(-15, 0, 0);
                    break;
                case "blade":
                    local = new Vector3(c.x, c.y, c.z - s.z * 0.38f);
                    break;
                case "knife":
                case "hilt":
                    local = new Vector3(c.x, c.y, c.z - s.z * 0.2f);
                    break;
            }
            if (!local.HasValue) return null;
            var grip = new GameObject("Grip").transform;
            grip.SetParent(root, false);
            grip.localPosition = local.Value;
            grip.localRotation = rot;
            return grip;
        }

        /// <summary>Calcula una BoxCollider que envuelve todos los renderers del visual.</summary>
        public static BoxCollider FitBoxCollider(GameObject go, Transform visual)
        {
            var renderers = visual.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return go.AddComponent<BoxCollider>();
            var inv = go.transform.worldToLocalMatrix;
            var b = new Bounds();
            bool init = false;
            foreach (var r in renderers)
            {
                if (r.name == "Liquido") continue;
                Mesh mesh = null;
                var mf = r.GetComponent<MeshFilter>();
                if (mf != null) mesh = mf.sharedMesh;
                else if (r is SkinnedMeshRenderer smr) mesh = smr.sharedMesh;
                if (mesh == null) continue;
                var mb = mesh.bounds;
                var m = inv * r.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    var corner = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var p = m.MultiplyPoint3x4(corner);
                    if (!init) { b = new Bounds(p, Vector3.zero); init = true; }
                    else b.Encapsulate(p);
                }
            }
            var box = go.AddComponent<BoxCollider>();
            box.center = b.center;
            box.size = Vector3.Max(b.size, Vector3.one * 0.03f);
            return box;
        }
    }
}
