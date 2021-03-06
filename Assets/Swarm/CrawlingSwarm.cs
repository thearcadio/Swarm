// Swarm - Special renderer that draws a swarm of swirling/crawling lines.
// https://github.com/keijiro/Swarm

using UnityEngine;
using Klak.Chromatics;
using DFVolume;

namespace Swarm
{
    public sealed class CrawlingSwarm : MonoBehaviour
    {
        #region Instancing properties

        [SerializeField] int _instanceCount = 1000;

        public int instanceCount {
            get { return _instanceCount; }
        }

        [SerializeField] TubeTemplate _template;

        public TubeTemplate template {
            get { return _template; }
        }

        [SerializeField] float _radius = 0.005f;

        public float radius {
            get { return _radius; }
            set { _radius = value; }
        }

        #endregion

        #region Dynamics properties

        [SerializeField] float _speed = 0.75f;

        public float speed {
            get { return _speed; }
            set { _speed = value; }
        }

        [SerializeField] VolumeData _volume;

        public VolumeData volume {
            get { return _volume; }
        }

        [SerializeField] float _noiseFrequency = 4;

        public float noiseFrequency {
            get { return _noiseFrequency; }
            set { _noiseFrequency = value; }
        }

        [SerializeField] float _noiseSpread = 0.5f;

        public float noiseSpread {
            get { return _noiseSpread; }
            set { _noiseSpread = value; }
        }

        [SerializeField] float _noiseMotion = 0.1f;

        public float noiseMotion {
            get { return _noiseMotion; }
            set { _noiseMotion = value; }
        }

        #endregion

        #region Material properties

        [SerializeField] Material _material;

        public Material material {
            get { return _material; }
        }

        [SerializeField] CosineGradient _gradient;

        public CosineGradient gradient {
            get { return _gradient; }
            set { _gradient = value; }
        }

        #endregion

        #region Hidden attributes

        [SerializeField, HideInInspector] ComputeShader _compute;

        #endregion

        #region Private fields

        ComputeBuffer _drawArgsBuffer;
        ComputeBuffer _positionBuffer;
        ComputeBuffer _tangentBuffer;
        ComputeBuffer _normalBuffer;
        MaterialPropertyBlock _props;
        int _frameCount;

        #endregion

        #region Compute configurations

        const int kThreadCount = 64;
        int ThreadGroupCount { get { return _instanceCount / kThreadCount; } }
        int InstanceCount { get { return kThreadCount * ThreadGroupCount; } }
        int HistoryLength { get { return _template.segments + 1; } }

        #endregion

        #region MonoBehaviour functions

        void OnValidate()
        {
            _instanceCount = Mathf.Max(kThreadCount, _instanceCount);
            _radius = Mathf.Max(0, _radius);
            _speed = Mathf.Max(0, _speed);
            _noiseFrequency = Mathf.Max(0, _noiseFrequency);
            _noiseSpread = Mathf.Max(0, _noiseSpread);
        }

        void Start()
        {
            // Initialize the indirect draw args buffer.
            _drawArgsBuffer = new ComputeBuffer(
                1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments
            );

            _drawArgsBuffer.SetData(new uint[5] {
                _template.mesh.GetIndexCount(0), (uint)InstanceCount, 0, 0, 0
            });

            // Allocate compute buffers.
            _positionBuffer = new ComputeBuffer(HistoryLength * InstanceCount, 16);
            _tangentBuffer = new ComputeBuffer(HistoryLength * InstanceCount, 16);
            _normalBuffer = new ComputeBuffer(HistoryLength * InstanceCount, 16);

            // Initialize the compute buffers.
            var kernel = _compute.FindKernel("CrawlingInit");
            _compute.SetInt("InstanceCount", InstanceCount);
            _compute.SetInt("HistoryLength", HistoryLength);
            _compute.SetTexture(kernel, "DFVolume", _volume.texture);
            _compute.SetBuffer(kernel, "PositionBuffer", _positionBuffer);
            _compute.SetBuffer(kernel, "TangentBuffer", _tangentBuffer);
            _compute.SetBuffer(kernel, "NormalBuffer", _normalBuffer);
            _compute.Dispatch(kernel, ThreadGroupCount, 1, 1);

            if (_props == null)
            {
                // This property block is used only for avoiding an instancing bug.
                _props = new MaterialPropertyBlock();
                _props.SetFloat("_UniqueID", Random.value);
            }
        }

        void OnDestroy()
        {
            _drawArgsBuffer.Release();
            _positionBuffer.Release();
            _tangentBuffer.Release();
            _normalBuffer.Release();
        }

        void Update()
        {
            var delta = Mathf.Min(Time.deltaTime, 1.0f / 30);

            if (delta > 0)
            {
                // Index offset on the position buffer.
                var offset0 = InstanceCount * ( _frameCount      % HistoryLength);
                var offset1 = InstanceCount * ((_frameCount + 1) % HistoryLength);
                var offset2 = InstanceCount * ((_frameCount + 2) % HistoryLength);

                // Invoke the update compute kernel.
                var kernel = _compute.FindKernel("CrawlingUpdate");

                _compute.SetInt("IndexOffset0", offset0);
                _compute.SetInt("IndexOffset1", offset1);
                _compute.SetInt("IndexOffset2", offset2);

                _compute.SetFloat("Speed", _speed * delta);
                _compute.SetFloat("NoiseFrequency", _noiseFrequency);
                _compute.SetFloat("NoiseSpread", _noiseSpread / InstanceCount);
                _compute.SetFloat("NoiseOffset", Time.time * _noiseMotion);

                _compute.SetTexture(kernel, "DFVolume", _volume.texture);
                _compute.SetBuffer(kernel, "PositionBuffer", _positionBuffer);

                _compute.Dispatch(kernel, ThreadGroupCount, 1, 1);

                // Invoke the reconstruction kernel.
                kernel = _compute.FindKernel("CrawlingReconstruct");

                _compute.SetBuffer(kernel, "PositionBufferRO", _positionBuffer);
                _compute.SetBuffer(kernel, "TangentBuffer", _tangentBuffer);
                _compute.SetBuffer(kernel, "NormalBuffer", _normalBuffer);

                _compute.Dispatch(kernel, ThreadGroupCount, 1, 1);
            }

            // Draw the mesh with instancing.
            _material.SetFloat("_Radius", _radius);

            _material.SetVector("_GradientA", _gradient.coeffsA);
            _material.SetVector("_GradientB", _gradient.coeffsB);
            _material.SetVector("_GradientC", _gradient.coeffsC2);
            _material.SetVector("_GradientD", _gradient.coeffsD2);

            _material.SetMatrix("_LocalToWorld", transform.localToWorldMatrix);
            _material.SetMatrix("_WorldToLocal", transform.worldToLocalMatrix);

            _material.SetBuffer("_PositionBuffer", _positionBuffer);
            _material.SetBuffer("_TangentBuffer", _tangentBuffer);
            _material.SetBuffer("_NormalBuffer", _normalBuffer);

            _material.SetInt("_IndexOffset", _frameCount + 3);
            _material.SetInt("_InstanceCount", InstanceCount);
            _material.SetInt("_HistoryLength", HistoryLength);

            Graphics.DrawMeshInstancedIndirect(
                _template.mesh, 0, _material,
                new Bounds(Vector3.zero, Vector3.one * 1.5f),
                _drawArgsBuffer, 0, _props
            );

            _frameCount++;
        }

        #endregion
    }
}
