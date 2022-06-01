 using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;
using SharpNoise;

public class WaterRenderer : MonoBehaviour {

    [System.Serializable]
    public class WaterSplashInfo {

        public float intensity;

        public float time;

        public float baseLocation;

        public Vector2 direction;

        public float currentLocation;

        public float size;

        public float extent;

        private float waveSpeed;

        public float minX;

        public float maxX;

        public float timeMod;

        private float splashTime;

        public WaterSplashInfo(float intensity, float location, Vector2 direction, float waveSpeed, float halfExtPerIntensity, float splashTime) {
            this.intensity = intensity;
            this.time = 0;
            this.baseLocation = location;
            this.direction = direction;
            currentLocation = location;
            this.waveSpeed = waveSpeed;
            this.extent = Mathf.Abs(intensity * halfExtPerIntensity);
            this.size = extent * 2;
            this.minX = currentLocation - extent;
            this.maxX = currentLocation + extent;
            timeMod = 0;
            this.splashTime = splashTime;
        }

        public void UpdateForTime(float dt) {
            this.time += dt;
            currentLocation = baseLocation + direction.x * time * waveSpeed;
            this.minX = currentLocation - extent;
            this.maxX = currentLocation + extent;

            this.timeMod = -4 * ((time - splashTime * 0.5f) / splashTime) * ((time - splashTime * 0.5f) / splashTime) + 1;
        }

    }

    [SerializeField]
    protected Vector2 size;

    [SerializeField]
    protected Color liquidColor;

    [SerializeField]
    protected Color edgeColor;

    [SerializeField]
    protected float upperMarginSize = 2;

    [SerializeField]
    protected float xStepSize = 1 / 8f;

    [SerializeField]
    protected float maxTimeStep = 1 / 60f;

    [SerializeField]
    protected float waveSpeed;

    [SerializeField]
    protected float damping;

    [SerializeField]
    protected float gravity;

    [SerializeField]
    protected float waveSpatFrequency;

    [SerializeField]
    protected float waveTimeFrequency;

    [SerializeField]
    protected float waveIntensity;

    [SerializeField]
    protected float splashTime;

    [SerializeField]
    protected float splashHalfExtentPerIntensity;

    [SerializeField]
    protected float splashSpeed;

    [SerializeField]
    protected float waveSize;

    //Simulation arrawys
    protected float[] w;
    protected float[] dw;
    protected float[] ddw;

    protected Color[] pixels;

    protected float invStepSq;

    protected float time;

    [SerializeField]
    protected List<WaterSplashInfo> splashes;

    protected MeshFilter mesh;
    protected MeshRenderer meshRenderer;

    [SerializeField]
    protected BoxCollider2D baseCollider;

    [SerializeField]
    protected BoxCollider2D captureCollider;

    protected Texture2D tex;

    protected PerlinNoise noise;

    protected float middle;

    [SerializeField]
    protected Particle2DDataSP foamData;

    [SerializeField]
    protected Vector2Int numFoamSplashIntensity;

    [SerializeField]
    protected Particle2DDataSP dropData;

    [SerializeField]
    protected Vector2Int numDropSplashIntensity;

    [SerializeField]
    protected Vector2 splashParticleSpread;

    protected void Start() {
        var numX = Mathf.CeilToInt(size.x / xStepSize);
        w = new float[numX];
        dw = new float[numX];
        ddw = new float[numX];
        pixels = new Color[numX];

        invStepSq = 1 / (xStepSize * xStepSize);
        time = 0;

        splashes = new List<WaterSplashInfo>();

        this.mesh = GetComponent<MeshFilter>();
        this.meshRenderer = GetComponent<MeshRenderer>();

        tex = new Texture2D(numX, 1, TextureFormat.Alpha8, false);

        this.mesh.mesh = BuildQuad();
        this.meshRenderer.material.SetColor("_WaterColor", liquidColor);
        this.meshRenderer.material.SetColor("_EdgeColor", edgeColor);
        this.meshRenderer.material.SetFloat("_XSize", (size.x + upperMarginSize) * 8);
        this.meshRenderer.material.SetFloat("_YSize", (size.y + upperMarginSize) * 8);
        this.meshRenderer.material.mainTexture = tex;

        captureCollider.offset = new Vector2(0, upperMarginSize / 2);
        captureCollider.size = new Vector2(size.x, size.y + upperMarginSize);

        baseCollider.size = size;

        noise = new PerlinNoise(GameManager.Instance.baseSeed + (uint) Mathf.FloorToInt(transform.position.x));

        middle = (size.y) / (size.y + upperMarginSize);

        for(int i = 0; i < 10; i++) {
            Simulate(0.0166f);
        }
    }

    protected Mesh BuildQuad() {
        var quad = new Mesh();
        var extents = Extents();
        extents.center = Vector3.zero;

        var verts = new List<Vector3> { extents.min, new Vector3(extents.min.x, extents.max.y + upperMarginSize, extents.min.z),
            new Vector3(extents.max.x, extents.max.y + upperMarginSize, extents.min.z), new Vector3(extents.max.x, extents.min.y, extents.min.z) };

        var uvs = new List<Vector2> { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) };

        quad.SetVertices(verts);
        quad.SetUVs(0, uvs);
        quad.SetIndices(new int[] { 0, 1, 2, 0, 2, 3 }, MeshTopology.Triangles, 0);

        quad.RecalculateNormals();
        quad.RecalculateBounds();

        return quad;
    }

    protected void Update() {
        if(Time.deltaTime == 0) {
            return;
        }

        var curdt = Time.deltaTime;
        if(curdt > maxTimeStep) {
            curdt = maxTimeStep;
        }

        Simulate(curdt);
    }

    protected void Simulate(float dt) {
        for(int i = splashes.Count - 1; i >= 0; i--) {
            splashes[i].UpdateForTime(dt);
            if(splashes[i].time > splashTime) {
                splashes.RemoveAt(i);
            }
        }

        var invTime = 1 / dt;

        time += dt;

        for(int i = 0; i < w.Length; i++) {
            if(i == 0) {
                dw[i] += dt * ((-2.5f * w[i] + 2 * w[i + 1]) * invStepSq * waveSpeed + ForcingFor(time, i * xStepSize) - w[i] * gravity);
            } else if(i == w.Length - 1) {
                dw[i] += dt * ((-2.5f * w[i] + 2 * w[i - 1]) * invStepSq * waveSpeed + ForcingFor(time, i * xStepSize) - w[i] * gravity);
            } else {
                dw[i] += dt * (((-2 * w[i] + 1 * (w[i - 1] + w[i + 1])) * invStepSq) * waveSpeed - damping * dw[i] * invTime + ForcingFor(time, i * xStepSize) - w[i] * gravity);
            }
        }

        for(int i = 0; i < w.Length; i++) {
            w[i] += dw[i] * dt;
            pixels[i] = new Color(0, 0, 0, (w[i] + size.y) / (size.y + upperMarginSize));
        }

        tex.SetPixels(pixels);
        tex.Apply();
    }

    private float ForcingFor(float t, float x) {
        var force = noise.GetNoise2D(x * waveSpatFrequency, t * waveTimeFrequency) * waveIntensity;

        foreach(var splash in splashes) {
            force += ForcingForSplash(t, x, splash);
        }

        return force;
    }

    private float ForcingForSplash(float t, float x, WaterSplashInfo splash) {
        const float twoPi = Mathf.PI * 2;
        const float halfPi = Mathf.PI * 0.5f;

        const float A = -1;
        const float b = -1;
        const float q = 2;

        if(x < splash.minX || x > splash.maxX) {
            return 0;
        }
        var xScaleShift = (x - splash.minX) / splash.size;
        
        float n = ((Mathf.Sin(xScaleShift * twoPi + halfPi) - 1) + A * (Mathf.Sin(xScaleShift * twoPi * q + halfPi) + b)) * waveSize * splash.timeMod * splash.intensity;
        n = n * 0.8f + n * noise.GetNoise2D(xScaleShift, splash.time + splash.intensity * 1000) * 0.5f;
        return n;
    }

    public void AddSplash(float intensity, Vector2 location, Vector2 direction) {
        splashes.Add(new WaterSplashInfo(intensity, location.x - Extents().min.x, direction, splashSpeed, splashHalfExtentPerIntensity, splashTime));
    }

    public Bounds Extents() {
        return new Bounds(transform.position, size);
    }

    public bool IsColliding(Vector2 point) {
        var ext = Extents();
        if(point.x < ext.min.x || point.x > ext.max.x) {
            return false;
        }

        var xIndex = Mathf.FloorToInt((point.x - ext.min.x) / xStepSize);
        
        if(xIndex < 0 || xIndex >= w.Length) {
            return false;
        }

        return (w[xIndex] + transform.position.y + ext.extents.y) > point.y;
    }

    public float GetHeightAt(Vector2 point) {
        var ext = Extents();
        if(point.x < ext.min.x || point.x > ext.max.x) {
            return -1;
        }

        var xIndex = Mathf.FloorToInt((point.x - ext.min.x) / xStepSize);

        if(xIndex < 0 || xIndex > w.Length) {
            return -1;
        }

        return w[xIndex] + transform.position.y + ext.extents.y;
    }

    protected void OnDrawGizmos() {
        Gizmos.color = new Color(0, 0, 1, 0.1f);
        Gizmos.DrawCube(transform.position, size);
    }

}
