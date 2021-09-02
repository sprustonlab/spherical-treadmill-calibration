# spherical-treadmill-calibration
 Tools for calibrating spherical treadmills
## Run calibration
* copy 'settings-template.yml' and rename to 'settings.yml' and fill out correct values.
# Check Cameras
* Make sure 'Matlab code' folder is added to the Matlab path
* Check video feed from cameras with:
    ```
    treadmillVideo('COM1') 
    ```
    
* Check ball movement with:
    ```
    motiondisplay('COM1') 
    ```
    (Adjust com port to your system)