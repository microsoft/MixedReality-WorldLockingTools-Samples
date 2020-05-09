//#define BARN_DISABLE_AT_END

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShowMarker : MonoBehaviour
{
    public float rampUp = 1.0f;

    public float rampDown = 5.0f;

    public float maxSize = 4.0f;

    private float age = 0.0f;

    private struct AnimMat
    {
        public Material material;

        public Color diffuse;

        public Color emissive;

    }
    private readonly SortedSet<AnimMat> materials = new SortedSet<AnimMat>();

    private Vector3 initScale;

    private readonly string diffuseName = "_Color";
    private readonly string emissiveName = "_EmissiveColor";

    // Start is called before the first frame update
    void Start()
    {
        var renderers = GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; ++i)
        {
            var rend = renderers[i];
            AnimMat animMat = new AnimMat()
            {
                material = rend.material,
                diffuse = rend.material.GetColor(diffuseName),
                emissive = rend.material.GetColor(emissiveName),
            };
            rend.material.SetColor(diffuseName, Color.black);
            SetIntensity(0.0f, animMat);
            initScale = transform.localScale;
            materials.Add(animMat);
        }
        age = 0.0f;
    }

    // Update is called once per frame
    void Update()
    {
        if (age < rampUp)
        {
            RampUp(age);
        }
        else if (age < rampUp + rampDown)
        {
            RampDown(age);
        }
        else
        {
            SetStatic();
        }
        age += Time.deltaTime;
    }

    private void SetStatic()
    {
#if BARN_DISABLE_AT_END
        gameObject.SetActive(false);
#else
        for (var iter = materials.GetEnumerator(); iter.MoveNext();)
        {
            AnimMat animMat = iter.Current;
            Color emissive = animMat.diffuse;
            animMat.material.SetColor(emissiveName, emissive);
        }
#endif
    }
    private float SmoothStep(float zero, float one, float t)
    {
        t = t - zero;
        t = t / (one - zero);
        if (t <= 0)
            return 0;
        if (t >= 1)
            return 1;
        return (3 - 2 * t) * t * t;
    }
    private void RampUp(float age)
    {
        float intensity = SmoothStep(0.0f, 1.0f, age / rampUp); 
        for (var iter = materials.GetEnumerator(); iter.MoveNext(); )
        {
            AnimMat animMat = iter.Current;
            SetIntensity(intensity, animMat);
        }
        SetSizeFromAge(age);
    }

    private void RampDown(float age)
    {
        float intensity = SmoothStep(0.0f, 1.0f, 1.0f - (age - rampUp) / rampDown);
        for (var iter = materials.GetEnumerator(); iter.MoveNext();)
        {
            AnimMat animMat = iter.Current;
            
            BlendIntensity(intensity, animMat);
        }
        SetSizeFromAge(age);
    }

    private void SetSizeFromAge(float age)
    {
        float t = age / (rampUp + rampDown);
        t *= 2.0f;
        t -= 1.0f;
        t = Mathf.Abs(t);
        t = 1.0f - t;
        t = 1.0f + Mathf.Pow(t - 1.0f, 3.0f);
        float minSize = 1.0f;
        float size = minSize + t * (maxSize - minSize);

        SetSize(size);
    }

    private void SetSize(float scale)
    {
        transform.localScale = initScale * scale;
    }

    private void SetIntensity(float intensity, AnimMat animMat)
    {
        Color emissive = animMat.emissive * intensity;
        animMat.material.SetColor(emissiveName, emissive);
    }

    private void BlendIntensity(float intensity, AnimMat animMat)
    {
        Color emissive = animMat.emissive * intensity + animMat.diffuse * (1.0f - intensity);
        animMat.material.SetColor(emissiveName, emissive);
    }

}
