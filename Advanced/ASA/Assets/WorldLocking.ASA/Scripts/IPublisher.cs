using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Microsoft.MixedReality.WorldLocking.ASA
{
    using CloudAnchorId = System.String;

    public class AnchoredObjectAndProperties
    {
        public GameObject anchorHanger;
        public IDictionary<string, string> properties;
    };

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// All poses are in Spongy space, that is the space of native anchors.
    /// </remarks>
    public interface IPublisher
    {
        public void Setup();

        public bool IsReady { get; }

        public Task<CloudAnchorId> Create(AnchoredObjectAndProperties pose);

        public Task<AnchoredObjectAndProperties> Read(CloudAnchorId cloudAnchorId);

        public Task<CloudAnchorId> Update(CloudAnchorId cloudAnchorId, AnchoredObjectAndProperties pose);

        public Task<Dictionary<CloudAnchorId, AnchoredObjectAndProperties>> Find(float radiusFromDevice);

        public Task PurgeArea(float radius);

        public Task Delete(CloudAnchorId cloudAnchorId);
    };

}