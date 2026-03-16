using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHitFlash : MonoBehaviour
{
    [Header("Flash Settings")]
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.12f;
    [SerializeField] private bool useEmissionFlash = true;
    [SerializeField] private Color emissionFlashColor = Color.white * 2f;

    [Header("Renderer References")]
    [SerializeField] private List<Renderer> targetRenderers = new List<Renderer>();

    private readonly List<Material[]> rendererMaterials = new List<Material[]>();
    private readonly Dictionary<Material, Color> originalBaseColors = new Dictionary<Material, Color>();
    private readonly Dictionary<Material, Color> originalColorColors = new Dictionary<Material, Color>();
    private readonly Dictionary<Material, Color> originalEmissionColors = new Dictionary<Material, Color>();

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private Coroutine flashRoutine;

    private void Awake()
    {
        if (targetRenderers.Count == 0)
        {
            targetRenderers.AddRange(GetComponentsInChildren<Renderer>());
        }

        rendererMaterials.Clear();
        originalBaseColors.Clear();
        originalColorColors.Clear();
        originalEmissionColors.Clear();

        for (int i = 0; i < targetRenderers.Count; i++)
        {
            Renderer currentRenderer = targetRenderers[i];
            if (currentRenderer == null) continue;

            Material[] materials = currentRenderer.materials;
            rendererMaterials.Add(materials);

            for (int m = 0; m < materials.Length; m++)
            {
                Material mat = materials[m];
                if (mat == null) continue;

                if (!originalBaseColors.ContainsKey(mat))
                {
                    if (mat.HasProperty(BaseColorId))
                        originalBaseColors.Add(mat, mat.GetColor(BaseColorId));
                    else
                        originalBaseColors.Add(mat, Color.white);
                }

                if (!originalColorColors.ContainsKey(mat))
                {
                    if (mat.HasProperty(ColorId))
                        originalColorColors.Add(mat, mat.GetColor(ColorId));
                    else
                        originalColorColors.Add(mat, Color.white);
                }

                if (!originalEmissionColors.ContainsKey(mat))
                {
                    if (mat.HasProperty(EmissionColorId))
                        originalEmissionColors.Add(mat, mat.GetColor(EmissionColorId));
                    else
                        originalEmissionColors.Add(mat, Color.black);
                }

                if (useEmissionFlash && mat.HasProperty(EmissionColorId))
                {
                    mat.EnableKeyword("_EMISSION");
                }
            }
        }
    }

    public void PlayFlash()
    {
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        ApplyFlash();
        yield return new WaitForSeconds(flashDuration);
        RestoreMaterials();
        flashRoutine = null;
    }

    private void ApplyFlash()
    {
        foreach (Material[] materialArray in rendererMaterials)
        {
            if (materialArray == null) continue;

            for (int i = 0; i < materialArray.Length; i++)
            {
                Material mat = materialArray[i];
                if (mat == null) continue;

                if (mat.HasProperty(BaseColorId))
                    mat.SetColor(BaseColorId, flashColor);

                if (mat.HasProperty(ColorId))
                    mat.SetColor(ColorId, flashColor);

                if (useEmissionFlash && mat.HasProperty(EmissionColorId))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor(EmissionColorId, emissionFlashColor);
                }
            }
        }
    }

    private void RestoreMaterials()
    {
        foreach (Material[] materialArray in rendererMaterials)
        {
            if (materialArray == null) continue;

            for (int i = 0; i < materialArray.Length; i++)
            {
                Material mat = materialArray[i];
                if (mat == null) continue;

                if (mat.HasProperty(BaseColorId) && originalBaseColors.ContainsKey(mat))
                    mat.SetColor(BaseColorId, originalBaseColors[mat]);

                if (mat.HasProperty(ColorId) && originalColorColors.ContainsKey(mat))
                    mat.SetColor(ColorId, originalColorColors[mat]);

                if (mat.HasProperty(EmissionColorId) && originalEmissionColors.ContainsKey(mat))
                    mat.SetColor(EmissionColorId, originalEmissionColors[mat]);
            }
        }
    }
}
