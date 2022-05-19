using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitPlugInAccessory
{
    public class OldTag //表示原有的标记信息记录类，用于标记重置位置不变
    {
        //标记Id
        public ElementId Id { get; set; }


        //标记主体Id
        public ElementId HostId { get; set; }

        //标记类型
        public ElementId TypeId { get; set; }


        //是否有引线
        public bool HasLeader { get; set; }

        public XYZ TagHeadPosition { get; set; }

        public XYZ LeaderEnd { get; set; }

        public XYZ LeaderElbow { get; set; }

        public OldTag(ElementId id, ElementId hostId, ElementId typeId, bool hasLeader, XYZ tagHeadPosition)
        {
            this.Id = id;
            this.HostId = hostId;
            this.TypeId = typeId;
            this.HasLeader = hasLeader;
            this.TagHeadPosition = tagHeadPosition;
        }

        public OldTag(ElementId id, ElementId hostId, ElementId typeId, bool hasLeader, XYZ tagHeadPosition, XYZ leaderEnd, XYZ leaderElbow)
        {
            this.Id = id;
            this.HostId = hostId;
            this.TypeId = typeId;
            this.HasLeader = hasLeader;
            this.TagHeadPosition = tagHeadPosition;
            this.LeaderEnd = leaderEnd;
            this.LeaderElbow = leaderElbow;
        }


        public OldTag(ElementId id, ElementId hostId, ElementId typeId, bool hasLeader, XYZ tagHeadPosition, XYZ leaderEnd)
        {
            this.Id = id;
            this.HostId = hostId;
            this.TypeId = typeId;
            this.HasLeader = hasLeader;
            this.TagHeadPosition = tagHeadPosition;
            this.LeaderEnd = leaderEnd;
        }

    }
}
