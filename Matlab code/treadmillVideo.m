function treadmillVideo(COMport,varargin)
%Fly Treadmill Video Acquisition Example
%
% TREADMILLVIDEO Simplified version of treadmill camera visualization tool
% using serial virtual com port interface.
%
% This software tool allows a user to see the 30x30 pixel field of view
% from each optical flow camera to assist in calibration, focusing,
% illumination, and alignment.  The update rate is 20Hz and is
% approximately as fast as the chips will offload their image data.  This
% is for experimental setup only.
%

%Cleanup from previously opened instances
delete(findobj('tag','gvid'))
delete(instrfind)
tic

%Create UI and image to dump values into
gui.fig=figure('tag','gvid','numbertitle','off','menubar','none','name','4-Axis Optical Motion Detection System');
centerfig(gui.fig)
% create axis.
gui.ax_im1 = subplot(2,2,1,'DataAspectRatio',[1,1,1]);
gui.ax_im2 = subplot(2,2,2,'DataAspectRatio',[1,1,1]);
gui.ax_hist1 = subplot(2,2,3);
gui.ax_hist2 = subplot(2,2,4);
% set blank images.
gui.im1=imshow( zeros(30,30),'Parent',gui.ax_im1,'DisplayRange',[0,100]);
gui.im2=imshow( zeros(30,30),'Parent',gui.ax_im2,'DisplayRange',[0,100]);
% set blank histogram.
gui.hist1 = histogram(gui.ax_hist1, zeros(60),20,'Normalization','probability');
gui.hist2 = histogram(gui.ax_hist2, zeros(60),20,'Normalization','probability');
% Set titles.
gui.ax_im1.Title.String = 'Camera 1 Image';
gui.ax_im2.Title.String = 'Camera 2 Image';
gui.ax_hist1.Title.String = 'Camera 1 Histogram';
gui.ax_hist1.XLabel.String = 'Pixel Value';
gui.ax_hist1.YLabel.String = 'Probability';
gui.ax_hist2.Title.String = 'Camera 2 Histogram';
gui.ax_hist2.XLabel.String = 'Pixel Value';
% add indicator light.
gui.sgt = sgtitle(sprintf('Frame Count: %i',0));
gui.frameCount = 0;
%Connect to serial object
s=serial(COMport);
s.baudrate=1250000;
s.inputbuffersize=50000;
fopen(s);

%Create control button
gui.vidgo=uicontrol('style','toggle','backgroundcolor','r','value',0,'string','Start Video','units','normalized','position',[0.01 0.01 0.15 0.05],'callback',{@vidstartstop,gui.fig,s});
set(gui.fig,'deletefcn',{@fdel,s})

set(gui.fig,'userdata',gui)

function vidstartstop(obj,event,fig,s)
gui=get(fig,'userdata');
switch get(obj,'value')
    case 0 %Stop Video
        set(obj,'backgroundcolor','r','string','Start Video');
        fwrite(s,[250,0]);
        pause(0.1);
        if s.bytesavailable>0
            fread(s,s.bytesavailable); 
        end
    case 1 %start Video
        gui.frameCount = 0;
        set(obj,'backgroundcolor','g','string','Stop Video');
        fclose(s)
        s.bytesavailablefcn={@baf,fig};
        s.bytesavailablefcncount=900*2; %one frame of pixels from each camera
        s.bytesavailablefcnmode='byte';
        fopen(s)
        fwrite(s,[251,0]);
        set(fig,'userdata',gui);
end

function fdel(obj,event,s)
    fwrite(s,[250,0]);
    fclose(s);
    delete(s);
    delete(obj);

function updatehist(xnew,h)
    [~,BinEdges] = histcounts(xnew,h.NumBins);
    BinLimits = [min(BinEdges),max(BinEdges)];
    h.Data= xnew;
    h.BinEdges= BinEdges;
    h.BinLimits= BinLimits;


%Fires every 1800 bytes (one frame from each camera).  MCU sends frames at 20Hz
function baf(obj,event,fig)

gui=get(fig,'userdata');
% update title.
gui.frameCount = gui.frameCount+1;
gui.sgt.String = sprintf('Frame Count: %i',gui.frameCount);
if obj.bytesavailable<obj.bytesavailablefcncount
    disp('returned')  %error checking
    return
end
%Grab a frame
raw=fread(obj,obj.bytesavailablefcncount);
if obj.bytesavailable>5000
    return;
end
raw0=raw(1:2:end);
raw1=raw(2:2:end);

%remove bit 7 from all bytes.  Remove bit 6 from first pixel
raw0=raw0-128; raw0(1)=raw0(1)-64; 
raw1=raw1-128; raw1(1)=raw1(1)-64;
raw0=raw0*2;% Johan: seems to scale to 0-100
raw1=raw1*2;
% plot image
gui.im1.CData = reshape(raw0,[30,30]); %Map linear vector into 30 colums by 30 rows
gui.im2.CData = reshape(raw1,[30,30]); %Map linear vector into 30 colums by 30 rows
% plot histogram
updatehist(raw0,gui.hist1)
updatehist(raw1,gui.hist2)
set(fig,'userdata',gui);
% toc
% drawnow




