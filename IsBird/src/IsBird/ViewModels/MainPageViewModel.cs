using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Acr.UserDialogs;
using IsBird.Interfaces;
using MvvmHelpers;
using Plugin.Media;
using Plugin.Media.Abstractions;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using Prism.AppModel;
using Prism.Commands;
using Prism.Navigation;
using Prism.Services;
using PropertyChanged;
using Xamarin.Forms;

namespace IsBird.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class MainPageViewModel : ViewModelBase
    {
        //Properties
        public string FinalWord { get; set; }
        public bool SpinnerOn { get; set; }
        public bool imageShowing { get; set; } = false;

        public string EntryText
        {
            get;
            set;
        }

        public ImageSource setImage { get; set; }
        public Stream selectedImage { get; set; }
        public DelegateCommand TakePhotoCommand { get; }
        IUserDialogs _userDialogs { get; }

        //Variables
        MediaFile file;
        const string newPhoto = "Take New Photo";

        public MainPageViewModel(INavigationService navigationService, IPageDialogService pageDialogService,
                                 IDeviceService deviceService, IUserDialogs userDialogs)
            : base(navigationService, pageDialogService, deviceService)
        {
            _userDialogs = userDialogs;
            TakePhotoCommand = new DelegateCommand(OnTakePhotoCommandExecuted);
        }

        //Make sure camera permissions are allowed and open the camera view
        public async void OnTakePhotoCommandExecuted()
        {
            var resultCam = await CheckOnCameraPermissions();
            var resultRoll = await CheckOnPhotoRollPermissions();
            if (resultCam && resultRoll)
            {
                await OpenPhotoTaker();
            }
            else
            {
                await _userDialogs.AlertAsync("Access Denied. Please fix Permissions");
            }
        }

        public async Task OpenPhotoTaker()
        {
            try
            {
                ActivateSpinner();
                var rndEnding = new Random().Next(100, 9999);
                if (!CrossMedia.Current.IsCameraAvailable || !CrossMedia.Current.IsTakePhotoSupported)
                {
                    await _userDialogs.AlertAsync("No camera functionality available", "Cannot Open Camera", "OK");
                    goto Exit;
                }

                var myfile = await CrossMedia.Current.TakePhotoAsync(new Plugin.Media.Abstractions.StoreCameraMediaOptions
                {
                    PhotoSize = PhotoSize.Small,                  //resizes the photo to 50% of the original
                    CompressionQuality = 92,                      // Int from 0 to 100 to determine image compression level
                    DefaultCamera = CameraDevice.Front,           // determine which camera to default to
                    Directory = "CC Directors",
                    Name = $"mybirdypic{rndEnding}",
                    SaveToAlbum = true,                                                     // this saves the photo to the camera roll 
                                                                                            //if we need the public album path --> var aPpath = file.AlbumPath;
                                                                                            //if we need a private path --> var path = file.Path;
                    AllowCropping = false,
                });

                file = myfile;
                if (file == null)
                {
                    goto Exit;
                }

                Debug.WriteLine("File Location:  " + file.Path);
                setImage = ImageSource.FromStream(() =>
                {
                    var stream = file.GetStream();
                    var path = file.Path;
                    var pathprivate = file.AlbumPath;
                    Debug.WriteLine(path);
                    Debug.WriteLine(pathprivate);
                    selectedImage = file.GetStream();

                    return stream;
                });
                imageShowing = true;
                RaisePropertyChanged();

                IsItBird(file);
                goto Exit;

            Exit:
                DeactivateSpinner();
                return;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            } finally
            {
                DeactivateSpinner();
            }
        }

        //Determine if the photo is of a bird
        public void IsItBird(MediaFile file)
        {
            var isItBirdy = Xamarin.Forms.DependencyService.Get<IBirdDetector>().GetBirdResults(file);
            var firstTag = isItBirdy.Item1;
            var confidenceLevel = isItBirdy.Item2;

            if (confidenceLevel.StartsWith("0", StringComparison.CurrentCulture))
            {
                firstTag = "i mean, i have no idea what this one is. i didn't ask for this";
                FinalWord = $"{firstTag}.\n & i'm about {confidenceLevel} sure.";
            }
            else
            {
                FinalWord = $"it's a {firstTag}.\n I'm about {confidenceLevel} sure.";
            }
        }

        #region CheckPermissions
        public async Task<bool> CheckOnCameraPermissions()
        {
            var cameraStatus = await CrossPermissions.Current.CheckPermissionStatusAsync(Permission.Camera);
            if (cameraStatus != PermissionStatus.Granted)
            {
                var results = await CrossPermissions.Current.RequestPermissionsAsync(new[] { Permission.Camera });
                cameraStatus = results[Permission.Camera];
            }

            if (cameraStatus == PermissionStatus.Granted)
            {
                return true;
            }
            return false;
        }

        public async Task<bool> CheckOnPhotoRollPermissions()
        {
            var storageStatus = await CrossPermissions.Current.CheckPermissionStatusAsync(Permission.Storage);
            if (storageStatus != PermissionStatus.Granted)
            {
                var results = await CrossPermissions.Current.RequestPermissionsAsync(new[] { Permission.Storage });
                storageStatus = results[Permission.Storage];
            }

            if (storageStatus == PermissionStatus.Granted)
            {
                return true;
            }
            return false;
        }
#endregion

        #region ActivityIndicator Helpers
        public void ActivateSpinner()
        {
            SpinnerOn = true;
            RaisePropertyChanged();
        }

        public void DeactivateSpinner()
        {
            SpinnerOn = false;
            RaisePropertyChanged();
        }
#endregion
    }
}