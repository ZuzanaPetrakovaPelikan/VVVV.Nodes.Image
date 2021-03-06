﻿#region using
using System.Collections.Generic;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using System;
using VVVV.Utils.VColor;

#endregion

namespace VVVV.Nodes.OpenCV
{
	#region Interfaces
	public abstract class CMPInstance : IFilterInstance
	{
		public double Threshold = 0.5;
		protected CVImage Buffer = new CVImage();

		private bool FPassOriginal = false;
		public bool PassOriginal
		{
			set
			{
				FPassOriginal = value;
				ReAllocate();
			}
		}

		public override void Allocate()
		{
			Buffer.Initialise(FInput.ImageAttributes.Size, TColorFormat.L8);
		}

		public override void Process()
		{
			if (FInput.ImageAttributes.ChannelCount == 1)
			{
				if (!FInput.LockForReading())
					return;
				try
				{
					Compare(FInput.CvMat);
				}
				finally
				{
					FInput.ReleaseForReading();
				}
			}
			else
			{
				FInput.GetImage(Buffer);
				Compare(Buffer.CvMat);
			}

			if (FPassOriginal)
				FOutput.Image.SetImage(FInput.Image);
			if (FPassOriginal)
			{
				CvInvoke.cvNot(Buffer.CvMat, Buffer.CvMat);
				CvInvoke.cvSet(FOutput.Image.CvMat, new MCvScalar(0.0), Buffer.CvMat);
				FOutput.Send();
			}
			else
				FOutput.Send(Buffer);
		}

		protected abstract void Compare(IntPtr CvMat);
	}

	public abstract class CMPNode<T> : IFilterNode<T> where T : CMPInstance, new()
	{
		[Input("Input 2", DefaultValue = 0.5)]
		IDiffSpread<double> FThreshold;

		[Input("Pass original", DefaultValue = 0)]
		IDiffSpread<bool> FPassOriginal;

		protected override void Update(int InstanceCount, bool SpreadChanged)
		{
			if (FThreshold.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Threshold = FThreshold[i];

			if (FPassOriginal.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].PassOriginal = FPassOriginal[i];
		}
	}
#endregion Interfaces

	#region Instances
	public class GTInstance : CMPInstance
	{
		protected override void Compare(IntPtr CvMat)
		{
			CvInvoke.cvCmpS(CvMat, Threshold, Buffer.CvMat, CMP_TYPE.CV_CMP_GT);
		}
	}

	public class LTInstance : CMPInstance
	{
		protected override void Compare(IntPtr CvMat)
		{
			CvInvoke.cvCmpS(CvMat, Threshold, Buffer.CvMat, CMP_TYPE.CV_CMP_LT);
		}
	}

	public class EQInstance : CMPInstance
	{
		protected override void Compare(IntPtr CvMat)
		{
			CvInvoke.cvCmpS(CvMat, Threshold, Buffer.CvMat, CMP_TYPE.CV_CMP_EQ);
		}
	}
	#endregion

	#region Nodes

	#region PluginInfo
	[PluginInfo(Name = ">", Help = "Greater than", Category = "OpenCV", Version = "Filter, Scalar")]
	#endregion PluginInfo
	public class GTNode : CMPNode<GTInstance>
	{	}

	#region PluginInfo
	[PluginInfo(Name = "<", Help = "Less than", Category = "OpenCV", Version = "Filter, Scalar")]
	#endregion PluginInfo
	public class LTNode : CMPNode<LTInstance>
	{	}

	#region PluginInfo
	[PluginInfo(Name = "=", Help = "Equal to", Category = "OpenCV", Version = "Filter, Scalar")]
	#endregion PluginInfo
	public class EQNode : CMPNode<EQInstance>
	{	}

	#endregion nodes
}
