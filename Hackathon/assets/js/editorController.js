app.controller('processDialogCtrl', function ($scope, $http,$interval, ngDialog) {
    $scope.Workingstatus = [];
    $scope.CheckEncodingStatus = null;
    $scope.CheckPostProcessingStatus = null;
    $scope.ShowFinalLink = false;
    var key = "";
    $scope.process = function () {
        var url = $scope.inputurl;
        if (url === null)
            alert("Provide url to public available Movie");
        $scope.isProcessDisabled = true;
        $scope.InitVideoEncoding("=" + url);
        $scope.Workingstatus.push("Creating Asset Folder");

    };
    $scope.QueryEncodingStatus = function () {
        $http.get('/api/mediaservices/' + key).then(function (result) {
            var data = JSON.parse(result.data);
            if (data.Status==="Processing") //nasty but works
            {
                var temp = $scope.Workingstatus.pop();
                temp += ".";
                $scope.Workingstatus.push(temp);
            }
            else if (data.Status ==="Finished")
            {
                
                $interval.cancel($scope.CheckEncodingStatus);
                $scope.CheckEncodingStatus = undefined;
                $scope.Workingstatus.push("Encoding finished");
                // Now start PostProcessing
                $scope.Workingstatus.push("Postprocessing will be started.");
                var paket = { IncomingUrl: $scope.inputurl, AssetUrl: data.AssetUri, LocatorUrl: data.UrlSmooth };
                $scope.InitPostProcessing("=" + JSON.stringify(paket));
            }
        }, function (data) {
            // error handling
        });


    }
    
    $scope.QueryPostProcessingStatus = function () {
        $http.get('/api/PostProcess/').then(function (result) {
            var data = result.data;
            if (data === "") //nasty but works
            {
                var temp = $scope.Workingstatus.pop();
                temp += ".";
                $scope.Workingstatus.push(temp);
            }
            else if (data !== "") {
                data = JSON.parse(data);
                if (data.status === 0)
                    $scope.Workingstatus.push(data.message);
                else if (data.status === 1)
                {
                    $scope.Workingstatus.push("Warning: " + data.message);
                }
                else if (data.status === 2) { //Success
                    $scope.finalLink = data.message;
                    $scope.ShowFinalLink = true;
                    if (angular.isDefined($scope.CheckPostProcessingStatus)) {
                        $interval.cancel($scope.CheckPostProcessingStatus);
                        $scope.CheckPostProcessingStatus = undefined;
                    }
                }
                else if (data.status === 3) {
                    alert("something really wrong - contact developer.");
                }
            }
            else {
                if (angular.isDefined($scope.CheckPostProcessingStatus)) {
                    $interval.cancel($scope.CheckPostProcessingStatus);
                    $scope.CheckPostProcessingStatus = undefined;
                }
                
            }
        }, function (data) {
            // error handling
        });


    }
    $scope.InitVideoEncoding = function(urltovideo)
    {   
        var config = { headers: {'Content-Type': 'application/x-www-form-urlencoded'}};
        $http.post('/api/mediaservices', urltovideo, config)
            .success(function (data, status, headers, config) {
                $scope.Workingstatus.push("Encoding Started");
                $scope.Workingstatus.push("encoding");
                key = data;
                $scope.CheckEncodingStatus = $interval($scope.QueryEncodingStatus, 3000);
            })
            .error(function (data, status, header, config) {
                $scope.Workingstatus.push("Encoding error");
            });
    }
    $scope.InitPostProcessing = function (containerurl) {
        var config = { headers: { 'Content-Type': 'application/x-www-form-urlencoded' } };
        $http.post('/api/PostProcess', containerurl, config)
            .success(function (data, status, headers, config) {
                $scope.Workingstatus.push("Postprocessing Started");
                $scope.CheckPostProcessingStatus = $interval($scope.QueryPostProcessingStatus, 500);
            })
            .error(function (data, status, header, config) {
                $scope.Workingstatus.push("PostProcessing error");
            });
    }
    
});

app.controller("EditorController", function ($scope, $http, ngDialog) {
   
    var Base64 = { _keyStr: "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=", encode: function (e) { var t = ""; var n, r, i, s, o, u, a; var f = 0; e = Base64._utf8_encode(e); while (f < e.length) { n = e.charCodeAt(f++); r = e.charCodeAt(f++); i = e.charCodeAt(f++); s = n >> 2; o = (n & 3) << 4 | r >> 4; u = (r & 15) << 2 | i >> 6; a = i & 63; if (isNaN(r)) { u = a = 64 } else if (isNaN(i)) { a = 64 } t = t + this._keyStr.charAt(s) + this._keyStr.charAt(o) + this._keyStr.charAt(u) + this._keyStr.charAt(a) } return t }, decode: function (e) { var t = ""; var n, r, i; var s, o, u, a; var f = 0; e = e.replace(/[^A-Za-z0-9+/=]/g, ""); while (f < e.length) { s = this._keyStr.indexOf(e.charAt(f++)); o = this._keyStr.indexOf(e.charAt(f++)); u = this._keyStr.indexOf(e.charAt(f++)); a = this._keyStr.indexOf(e.charAt(f++)); n = s << 2 | o >> 4; r = (o & 15) << 4 | u >> 2; i = (u & 3) << 6 | a; t = t + String.fromCharCode(n); if (u !== 64) { t = t + String.fromCharCode(r) } if (a !== 64) { t = t + String.fromCharCode(i) } } t = Base64._utf8_decode(t); return t }, _utf8_encode: function (e) { e = e.replace(/rn/g, "n"); var t = ""; for (var n = 0; n < e.length; n++) { var r = e.charCodeAt(n); if (r < 128) { t += String.fromCharCode(r) } else if (r > 127 && r < 2048) { t += String.fromCharCode(r >> 6 | 192); t += String.fromCharCode(r & 63 | 128) } else { t += String.fromCharCode(r >> 12 | 224); t += String.fromCharCode(r >> 6 & 63 | 128); t += String.fromCharCode(r & 63 | 128) } } return t }, _utf8_decode: function (e) { var t = ""; var n = 0; var r = c1 = c2 = 0; while (n < e.length) { r = e.charCodeAt(n); if (r < 128) { t += String.fromCharCode(r); n++ } else if (r > 191 && r < 224) { c2 = e.charCodeAt(n + 1); t += String.fromCharCode((r & 31) << 6 | c2 & 63); n += 2 } else { c2 = e.charCodeAt(n + 1); c3 = e.charCodeAt(n + 2); t += String.fromCharCode((r & 15) << 12 | (c2 & 63) << 6 | c3 & 63); n += 3 } } return t } }
    $scope.LoadXml = function () {
        var config = { headers: { 'Content-Type': 'application/x-www-form-urlencoded' } };
        $http.post('/api/Config', "=" + Base64.encode($scope.metaxml), config).success(function (result) {
            var wData = JSON.parse(result);
            $scope.HideEditorPane = false;
            $scope.setViewData(wData);

            }, function (data) {
                alert("ErrorCode: " + data.status);
            })
    }
    $scope.HideEditorPane = true;
    $scope.open = function () {

        ngDialog.open({
            template: 'processDialog',
            controller: 'processDialogCtrl',
            className: 'ngdialog-theme-default ngdialog-theme-custom',
            closeByDocument: false
        });
    }

    $scope.setViewData = function (data) {
        $scope.viewData = data
        $scope.filterList = ["Normal", "Low"]
        $scope.fullCaption = ""
        $scope.newKeyword = ""
        $scope.setViewPlayer(data.ISMUrl)
    }

    $scope.filterConfidence = function (item) {
        return $scope.filterList.indexOf(item.Phrase[0].Confidence) >= 0 ? true : false
    }

    $scope.setViewPlayer = function (url) {
        var markers = [];

        if ($scope.viewData.DetectedPhrases === null)
            return;

        angular.forEach($scope.viewData.DetectedPhrases, function (value) {
            $scope.fullCaption += value.Phrase[0].Content + String.fromCharCode(13, 10) + String.fromCharCode(13, 10)
            if (value.Phrase[0].Confidence !== 'High')
                this.push(value.Phrase[0].Offset)
        }, markers)

        var myOptions = {
            autoplay: true,
            controls: true,
            width: "1024",
            height: "480",
            plugins: {
                timelineMarker: {
                    markertime: markers
                }
            }
        }

        $scope.player = amp("azuremediaplayer", myOptions,
            function () {
                this.addEventListener('timeupdate', function () {
                    $scope.progress()
                })
            });

        // src should be retrieved from JSON
        $scope.player.src([{ src: url+"/manifest", type:"application/vnd.ms-sstr+xml" }])

        // <source src="http://amssamples.streaming.mediaservices.windows.net/91492735-c523-432b-ba01-faba6c2206a2/AzureMediaServicesPromo.ism/manifest" type="application/vnd.ms-sstr+xml" />
    }

    $scope.progress = function () {
        // handle updates for every timeupdate event
    }

    $scope.setTimeAndCaption = function (item) {
        var timeFragments = item.Offset.split(":")
        var offsetInSeconds = (parseInt(timeFragments[0], 10) * 60 * 60) + (parseInt(timeFragments[1], 10) * 60) + parseInt(timeFragments[2], 10)
        $scope.player.currentTime(offsetInSeconds + 1)
        $scope.currentCaptionContent = item.Content
        $scope.currentCaptionTimecode = item.Offset
    }

    $scope.updateCaption = function () {
        var x = $scope.currentCaptionContent
        var y = $scope.currentCaptionTimecode
        if ($scope.viewData.DetectedPhrases === null)
            return;
        for (var item in $scope.viewData.DetectedPhrases) {
            if ($scope.viewData.DetectedPhrases[item].Phrase[0].Offset === $scope.currentCaptionTimecode)
                $scope.viewData.DetectedPhrases[item].Phrase[0].Content = $scope.currentCaptionContent
        }
    }

    $scope.addKeyword = function () {
        if ($scope.newKeyword !== "") {
            $scope.viewData.Entities.push({ "Entity": $scope.newKeyword, "Accepted": true })
            $scope.newKeyword = ""
        }
    }

    $scope.flagKeyword = function (item) {
        item.Accepted = !item.Accepted
    }

    $scope.refreshFull = function () {
        $scope.fullCaption = ""
        if ($scope.viewData.DetectedPhrases === null)
            return;
        angular.forEach($scope.viewData.DetectedPhrases, function (value) {
            $scope.fullCaption += value.Phrase[0].Content + String.fromCharCode(13, 10) + String.fromCharCode(13, 10)})
    }
});