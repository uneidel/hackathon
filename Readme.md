## Hackathon Media Services - Cognitive Services 

This Sample Code contains a Azure AppService containing an ASP.net WebSite(including a angularjs SPA) & Asp Web Job analyzing  Videos.


####Current Workflow:
1) Encode Video       
2) Split Audio (using ffmpeg to find silence Sections) - Splitting currently done with naudio.        
3) Each < 20 sec Chunk will be passed to Azure Cognitive Services Speech to Text API returning pure Text and Confidence (this cannot be done by LongDictiation Mode)    
4) After all text is returned keywords and Entities are extracted  via Cognitive Services.       
5) Face Recognition updated -> Two step Workflow:      
		Each 10 Seconds a Screenshot is done (please see preset.json for adjust)            
		Face Detect will identity all Faces (up to 64) in each Picture          
		Vision Api (Celebrity Domain model) identifies all Celebrities        
		Comparison each FaceRectangle to split detected Faces and unrecognized Faces      
		(Note: Dectected Faces are only shown by Name, unrecognized are base64 encoded in json)       



#####Architecture: 
![alt text](https://github.com/uneidel/hackathon/blob/master/Architecture.PNG "Architecture")

#####Screenshots:
![alt text](https://github.com/uneidel/hackathon/blob/master/Editor.PNG "Editor")
![alt text](https://github.com/uneidel/hackathon/blob/master/ProcessVideo1.PNG "Process")
![alt text](https://github.com/uneidel/hackathon/blob/master/FaceVision.PNG "FaceVision")

#####Requirements:
 - Azure Subscription
 - Media Services (including Encoding Unit, Streaming Unit)
 - Storage Account 
 - Cognitive services including Speech API, TextAnalysis, Keyword Extraction, Face API, Vision Api




 ###Neccessary Configuration:
 
 ####Hackathon web.config: 
 add key="MediaServicesAccountName" value=""/>       
 add key="MediaServicesAccountKey" value=""/>       
 add key="MediaServicesStorageAccountName" value="" />      
 add key="MediaServicesStorageAccountKey" value="" />      

 ####HackathonBGWorker app.config:
 add key="languageCode" value="de-de" />      
 add key="ttsSubscriptionKey" value="" />      
 add key="TextAnalysisKey" value="" />      
 add key="EntityLinkingKey" value=""/>     


 ###Download FFMPeg from https://www.ffmpeg.org and place it into HackathonnBGWorker Tools Folder (Please set Copy if newer -> Always) 



## Contribution
Daniel.Kreuzhofer@microsoft.com, sv@daenet.de



The MIT License (MIT)
Copyright (c) 2016 uneidel

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
