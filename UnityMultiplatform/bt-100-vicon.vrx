<?xml version="1.0" ?>
<MiddleVR>
    <Kernel LogLevel="2" LogInSimulationFolder="0" EnableCrashHandler="0" Version="1.4.0.f2" />
    <DeviceManager WandAxis="Mouse.Axis" WandHorizontalAxis="0" WandHorizontalAxisScale="1" WandVerticalAxis="1" WandVerticalAxisScale="1" WandButtons="Mouse.Buttons" WandButton0="0" WandButton1="1" WandButton2="2" WandButton3="3" WandButton4="4" WandButton5="5">
        <Driver Type="vrDriverDirectInput" />
        <Driver Type="vrDriverVRPN">
            <Tracker Address="bt100@130.179.30.223" ChannelIndex="0" ChannelsNb="1" Name="ViconHead" Right="X" Front="Y" Up="Z" NeutralPosition="0.000000,0.000000,0.000000" WaitForData="0" />
            <Tracker Address="uwand@130.179.30.223" ChannelIndex="0" ChannelsNb="1" Name="ViconWand" Right="X" Front="Y" Up="Z" NeutralPosition="0.000000,0.000000,0.000000" WaitForData="0" />
        </Driver>
    </DeviceManager>
    <DisplayManager Fullscreen="0" WindowBorders="0" ShowMouseCursor="0" VSync="0" AntiAliasing="0" ForceHideTaskbar="0" SaveRenderTarget="0">
        <Node3D Name="CenterNode" Parent="VRRootNode" Tracker="0" PositionLocal="0.000000,0.000000,0.000000" OrientationLocal="0.000000,0.000000,0.000000,1.000000" />
        <Node3D Name="HeadNode" Tag="Head" Parent="CenterNode" Tracker="0" PositionLocal="0.000000,0.000000,0.000000" OrientationLocal="0.000000,0.000000,0.000000,1.000000" />
        <CameraStereo Name="CameraStereo0" Parent="HeadNode" Tracker="ViconHead.Tracker0" UseTrackerX="1" UseTrackerY="1" UseTrackerZ="1" UseTrackerYaw="1" UseTrackerPitch="1" UseTrackerRoll="1" VerticalFOV="11.276" Near="0.1" Far="1000" Screen="0" ScreenDistance="0.585" UseViewportAspectRatio="1" AspectRatio="1.33333" InterEyeDistance="0.065" LinkConvergence="1" />
        <Node3D Name="Wand" Parent="CenterNode" Tracker="ViconWand.Tracker0" UseTrackerX="1" UseTrackerY="1" UseTrackerZ="1" UseTrackerYaw="1" UseTrackerPitch="1" UseTrackerRoll="1" />
        <Viewport Name="Viewport0" Left="-960" Top="0" Width="960" Height="540" Camera="CameraStereo0" Stereo="1" StereoMode="3" CompressSideBySide="1" StereoInvertEyes="0" OculusRiftWarping="0" />
    </DisplayManager>
    <ClusterManager NVidiaSwapLock="0" DisableVSyncOnServer="1" ForceOpenGLConversion="0" BigBarrier="0" SimulateClusterLag="0" />
</MiddleVR>
