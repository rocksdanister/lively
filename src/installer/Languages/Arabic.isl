; *** Inno Setup version 6.1.0+ arabic messages ***
;
; Translated by nacer baaziz (nacerstile@gmail.com)
;   http://www.jrsoftware.org/files/istrans/
;
; Note: When translating this text, do not add periods (.) to the end of
; messages that didn't have them already, because on those messages Inno
; Setup adds the periods automatically (appending a period would result in
; two periods being displayed).

[LangOptions]
; The following three entries are very important. Be sure to read and 
; understand the '[LangOptions] section' topic in the help file.
LanguageName=arabic
LanguageID=$0401
LanguageCodePage=0
RightToLeft=yes
; If the language you are translating to requires special font faces or
; sizes, uncomment any of the following entries and change them accordingly.
;DialogFontName=
;DialogFontSize=8
;WelcomeFontName=Verdana
;WelcomeFontSize=12
;TitleFontName=Arial
;TitleFontSize=29
;CopyrightFontName=Arial
;CopyrightFontSize=8

[Messages]

; *** Application titles
SetupAppTitle=إعداد
SetupWindowTitle=إعداد - %1
UninstallAppTitle=إزالة التثبيت
UninstallAppFullTitle=إزالة تثبيت %1

; *** Misc. common
InformationTitle=معلومات
ConfirmTitle=تأكيد
ErrorTitle=خطأ

; *** SetupLdr messages
SetupLdrStartupMessage=هذا المعالج سيقوم بتثبيت %1. هل تريد المتابعة?
LdrCannotCreateTemp=تعذر إنشاء الملفات المؤقتة, تم فشل معالج التثبيت.
LdrCannotExecTemp=تعذر تشغيل الملفات من المجلد المؤقت. فشل معالج التثبيت.
HelpTextNote=

; *** Startup error messages
LastErrorMessage=%1.%n%n خطأ %2: %3
SetupFileMissing=الملف %1 مفقود من دليل التثبيت. الرجاء تصحيح المشكلة أو الحصول على نسخة جديدة من البرنامج.
SetupFileCorrupt=ملفات الإعداد تالفة. الرجاء الحصول على نسخة جديدة من البرنامج.
SetupFileCorruptOrWrongVer=ملفات الإعداد تالفة أو غير متوافقة مع هذا الإصدار من برنامج الإعداد. الرجاء تصحيح المشكلة أو الحصول على نسخة جديدة من البرنامج.
InvalidParameter=تم تمرير أوامر غير صالحة على سطر الأوامر : %n%n%1
SetupAlreadyRunning=برنامج الإعداد قيد التشغيل بالفعل.
WindowsVersionNotSupported=لا يدعم هذا البرنامج إصدار Windows الذي يعمل به الكمبيوتر.
WindowsServicePackRequired=هذا البرنامج يتطلب %1 حزمة الخدمة %2 أو أعلى.
NotOnThisPlatform=لن يتم تشغيل هذا البرنامج على %1.
OnlyOnThisPlatform=يجب تشغيل هذا البرنامج على %1.
OnlyOnTheseArchitectures=يمكن تثبيت هذا البرنامج فقط على إصدارات Windows المصممة لهندسة المعالج التالية : %n%n%1
WinVersionTooLowError=هذا البرنامج يتطلب %1 الإصدار %2 أو أعلى.
WinVersionTooHighError=لا يمكن تثبيت هذا البرنامج على %1 الإصدار %2 أو أعلى.
AdminPrivilegesRequired=يجب أن يتم تسجيل دخولك كمسؤول عند تثبيت هذا البرنامج.
PowerUserPrivilegesRequired=يجب أن يتم تسجيل دخولك كمسؤول أو كعضو في مجموعة Power Users عند تثبيت هذا البرنامج.
SetupAppRunningError=لقد كشف معالج الإعداد أن  %1 يعمل بالفعل. %n%n يرجى إغلاق كل أجزائه الآن , ثم إضغط حسنا للمتابعة أو إلغاء الأمر للخروج.
UninstallAppRunningError=كشف معالج إلغاء التثبيت بأن %1 يعمل بالفعل.%n%n يرجى إغلاق كل أجزائه الآن , ثم إضغط حسنا للمتابعة أو إلغاء الأمر للخروج.

; *** Startup questions
PrivilegesRequiredOverrideTitle=تحديد وضع تثبيت الإعداد
PrivilegesRequiredOverrideInstruction=تحديد وضع التثبيت
PrivilegesRequiredOverrideText1=يمكن ل %1 أن يُثَبَّت على جميع المستخدمين (يتطلب إمتيازات المسؤول), أو لك فقط..
PrivilegesRequiredOverrideText2=.يمكن ل %1  أن يُثَبَّت لك فقط, أو أن يُثَبَّت على جميع المستخدمين (يتطلب إمتيازات المسؤول).
PrivilegesRequiredOverrideAllUsers=التثبيت ل&كافة المستخدمين
PrivilegesRequiredOverrideAllUsersRecommended=تثبيت ل&كافة المستخدمين (مستحسن)
PrivilegesRequiredOverrideCurrentUser=تثبيت لي &فقط
PrivilegesRequiredOverrideCurrentUserRecommended=تثبيت بالنسبة لي &فقط (مستحسن)

; *** Misc. errors
ErrorCreatingDir=تعذر على برنامج الإعداد إنشاء الدليل "%1"
ErrorTooManyFilesInDir=تعذر إنشاء ملف في الدليل  "%1" لأنه يحتوي على ملفات كثيرة جداً

; *** Setup common messages
ExitSetupTitle=الخروج من معالج التثبيت
ExitSetupMessage=لم يكتمل الإعداد. إذا قمت بالخروج الآن، لن يتم تثبيت البرنامج.%n%nYou يمكنك تشغيل برنامج الإعداد مرة أخرى في وقت آخر لإكمال التثبيت.%n%n إنهاء الإعداد؟
AboutSetupMenuItem=&حول الإعداد...
AboutSetupTitle=حول برنامج الإعداد
AboutSetupMessage=%1 الإصدار %2%n%3%n%n%1 صفحة الأنترنت:%n%4
AboutSetupNote=
TranslatorNote=تم ترجمة المعالج إلى اللغة العربية بواسطة ناصر بعزيز

; *** Buttons
ButtonBack=< ال&سابق
ButtonNext=ال&تالي >
ButtonInstall=&تثبيت
ButtonOK=&حسنا
ButtonCancel=إل&غاء الأمر
ButtonYes=&نعم
ButtonYesToAll=نعم لل&كل
ButtonNo=&لا
ButtonNoToAll=لا &للكل
ButtonFinish=إ&نهاء
ButtonBrowse=اس&تعراض...
ButtonWizardBrowse=اس&تعراض...
ButtonNewFolder=إن&شاء مجلد جديد

; *** "Select Language" dialog messages
SelectLanguageTitle=إختر لغة معالج الإعداد
SelectLanguageLabel=حدد اللغة التي يجب استخدامها أثناء التثبيت.

; *** Common wizard text
ClickNext=انقر فوق التالي للمتابعة، أو إلغاء الأمر لإنهاء الإعداد.
BeveledLabel=
BrowseDialogTitle=تصفح لاختيار مجلد
BrowseDialogLabel=حدد مجلدًا في القائمة أدناه، ثم انقر فوق حسنا.
NewFolderName=مجلد جديد

; *** "Welcome" wizard page
WelcomeLabel1=مرحبا بكم في معالج تثبيت [name]
WelcomeLabel2=هذا المعالج سيقوم بتثبيت  [name/ver] على جهازك. %n%nمن المستحسن أن تقوم بإغلاق كافة التطبيقات الأخرى قبل المتابعة.

; *** "Password" wizard page
WizardPassword=كلمة السر
PasswordLabel1=هذا التثبيت محمي بكلمة سر.
PasswordLabel3=الرجاء تقديم كلمة المرور، ثم انقر فوق التالي للمتابعة. كلمات المرور حساسة لحالة الأحرف.
PasswordEditLabel=&كلمة السر:
IncorrectPassword=كلمة السر التي أدخلتها غير صحيحة. يرجى إعادة المحاولة.

; *** "License Agreement" wizard page
WizardLicense=اتفاقية الترخيص
LicenseLabel=يرجى قراءة المعلومات الهامة التالية قبل المتابعة.
LicenseLabel3=الرجاء قراءة اتفاقية الترخيص التالية. يجب قبول شروط هذه الاتفاقية قبل متابعة التثبيت.
LicenseAccepted=أنا أواف&ق على هذه الإتفاقية
LicenseNotAccepted=أنا &لا أوافق على الإتفاقية

; *** "Information" wizard pages
WizardInfoBefore=معلومات
InfoBeforeLabel=يرجى قراءة المعلومات الهامة التالية قبل المتابعة.
InfoBeforeClickLabel=عندما تكون جاهزًا للمتابعة مع الإعداد، انقر فوق التالي.
WizardInfoAfter=معلومات
InfoAfterLabel=يرجى قراءة المعلومات الهامة التالية قبل المتابعة.
InfoAfterClickLabel=عندما تكون جاهزًا للمتابعة مع الإعداد، انقر فوق التالي.

; *** "User Information" wizard page
WizardUserInfo=معلومات المستخدم
UserInfoDesc=يرجى إدخال معلوماتك.
UserInfoName=إسم ال&مستخدم :
UserInfoOrg=المن&ظمة:
UserInfoSerial=&الرقم التسلسلي:
UserInfoNameRequired=يجب إدخال إسم.

; *** "Select Destination Location" wizard page
WizardSelectDir=تحديد موقع الوِجْهة
SelectDirDesc=أين يجب تثبيت [name]؟
SelectDirLabel3=سيقوم معالج التثبيت بتثبيت  [name] في المجلد التالي.
SelectDirBrowseLabel=للمتابعة، انقر فوق التالي. إذا كنت ترغب في تحديد مجلد آخر، انقر فوق استعراض.
DiskSpaceGBLabel=تحتاج على الأقل [gb] GB من المساحة لتثبيت البرنامج. 
DiskSpaceMBLabel=تحتاج على الأقل [mb] MB من المساحة لتثبيت البرنامج.
CannotInstallToNetworkDrive=يتعذر على برنامج الإعداد التثبيت على محرك أقراص شبكة اتصال.
CannotInstallToUNCPath=يتعذر على برنامج الإعداد تثبيت مسار UNC.
InvalidPath=يجب إدخال مسار كامل مع حرف محرك الأقراص; على سبيل المثال: %n%nC:\APP%n%أو مسار UNC في النموذج:%n%n\\server\share
InvalidDrive=محرك الأقراص أو مشاركة UNC التي حددتها غير موجود أو غير قابل للوصول. الرجاء تحديد آخر.
DiskSpaceWarningTitle=مساحة القرص غير كافية
DiskSpaceWarning=Sيتطلب الإعداد على الأقل %1 KB من المساحة الفارغة للتثبيت، ولكن محرك الأقراص المحدد فيه فقط %2 KB متوفرة.%n%nهل تريد المتابعة على أية حال؟
DirNameTooLong=اسم المجلد أو المسار طويل جداً.
InvalidDirName=اسم المجلد غير صالح.
BadDirName32=لا يمكن لأسماء المجلدات تضمين أي من الأحرف التالية:%n%n%1
DirExistsTitle=المجلد موجود بالفعل
DirExists=المجلد:%n%n%1%n%n موجود بالفعل. هل ترغب في التثبيت على هذا المجلد على أي حال؟
DirDoesntExistTitle=المجلد غير موجود
DirDoesntExist=المجلد:%n%n%1%n%nغير موجود. هل تريد إنشاء المجلد؟

; *** "Select Components" wizard page
WizardSelectComponents=تحديد المكونات
SelectComponentsDesc=ما هي المكونات التي يجب تثبيتها؟
SelectComponentsLabel2=حدد المكونات التي تريد تثبيتها ؛ امسح المكونات التي لا تريد تثبيتها. انقر فوق "التالي" عندما تكون مستعدًا للمتابعة.
FullInstallation=تثبيت كامل
; if possible don't translate 'Compact' as 'Minimal' (I mean 'Minimal' in your language)
CompactInstallation=تثبيت مدمج
CustomInstallation=تثبيت مخصص
NoUninstallWarningTitle=مكونات موجودة
NoUninstallWarning=اكتشف برنامج الإعداد أن المكونات التالية مثبتة بالفعل على جهاز الكمبيوتر الخاص بك: %n%n%1%n%nلن يؤدي إلغاء تحديد هذه المكونات إلى إزالة تثبيتها.%n%nهل ترغب في الاستمرار على أي حال?
ComponentSize1=%1 KB
ComponentSize2=%1 MB
ComponentsDiskSpaceGBLabel=الاختيار الحالي يتطلب على الأقل [gb] GB من مساحة القرص.
ComponentsDiskSpaceMBLabel=الاختيار الحالي يتطلب على الأقل [mb] MB من مساحة القرص.

; *** "Select Additional Tasks" wizard page
WizardSelectTasks=حدد المهام الإضافية
SelectTasksDesc=ما المهام الإضافية التي ينبغي تنفيذها؟
SelectTasksLabel2=حدد المهام الإضافية التي ترغب في أن يقوم الإعداد بتنفيذها أثناء تثبيت [name], ثم إضغط التالي.

; *** "Select Start Menu Folder" wizard page
WizardSelectProgramGroup=حدد مجلد قائمة ابدأ
SelectStartMenuFolderDesc=أين يجب أن يضع الإعداد اختصارات البرنامج؟
SelectStartMenuFolderLabel3=سيقوم برنامج الإعداد بإنشاء اختصارات البرنامج في مجلد قائمة ابدأ التالية.
SelectStartMenuFolderBrowseLabel=للمتابعة، انقر فوق التالي. إذا كنت ترغب في تحديد مجلد آخر، انقر فوق استعراض.
MustEnterGroupName=يجب إدخال اسم مجلد.
GroupNameTooLong=اسم المجلد أو المسار طويل جداً.
InvalidGroupName=اسم المجلد غير صالح.
BadGroupName=لا يمكن أن يتضمن اسم المجلد أي من الأحرف التالية:%n%n%1
NoProgramGroupCheck2=&عدم إنشاء مجلد قائمة ابدأ

; *** "Ready to Install" wizard page
WizardReady=جاهز للتثبيت
ReadyLabel1=الإعداد جاهز الآن لبدء تثبيت [name] على جهازك.
ReadyLabel2a=انقر فوق تثبيت لمتابعة التثبيت، أو انقر فوق "السابق" إذا كنت ترغب في مراجعة أو تغيير أية إعدادات.
ReadyLabel2b=انقر فوق تثبيت لمتابعة التثبيت.
ReadyMemoUserInfo=معلومات المستخدم:
ReadyMemoDir=مسار الوِجْهة:
ReadyMemoType=نوع الإعداد:
ReadyMemoComponents=المكونات المحددة:
ReadyMemoGroup=مجلد قائمة ابدأ:
ReadyMemoTasks=مهام إضافية:

; *** TDownloadWizardPage wizard page and DownloadTemporaryFile
DownloadingLabel=تحميل الملفات الإضافية...
ButtonStopDownload=إي&قاف التحميل
StopDownload=هل أنت متأكد من أنك ترغب في إيقاف التحميل؟
ErrorDownloadAborted=تم إلغاء التحميل
ErrorDownloadFailed=فشل التحميل: %1 %2
ErrorDownloadSizeFailed=خطأ في قراءة الحجم: %1 %2
ErrorFileHash1=خطأ في قراءة الهاش الخاص بالملف: %1
ErrorFileHash2=خطأ في هاش الملف: كان من المتوقع أن يكن : %1, بينما تم إيجاد : %2
ErrorProgress=خطأ في الحصول على نسبة التقدم: %1 من %2
ErrorFileSize=خطأ في حجم الملف: المتوقع هو : %1, الحجم الذي وجدناه هو : %2

; *** "Preparing to Install" wizard page
WizardPreparing=التحضير للتثبيت
PreparingDesc=الإعداد يستعد لتثبيت [name] على جهازك.
PreviousInstallNotCompleted=لم يكتمل التثبيت / إزالة البرنامج السابق. ستحتاج إلى إعادة تشغيل الكمبيوتر لإكمال هذا التثبيت.%n%nبعد إعادة تشغيل جهاز الكمبيوتر الخاص بك ، شغّل برنامج الإعداد مرة أخرى لإكمال تثبيت [name].
CannotContinue=لا يمكن لبرنامج الإعداد المتابعة. يرجى النقر فوق "إلغاء" للخروج.
ApplicationsFound=تستخدم التطبيقات التالية الملفات التي تحتاج إلى تحديث بواسطة برنامج الإعداد. يوصى بالسماح لبرنامج الإعداد بإغلاق هذه التطبيقات تلقائيًا.
ApplicationsFound2=تستخدم التطبيقات التالية الملفات التي تحتاج إلى تحديث بواسطة برنامج الإعداد. يوصى بالسماح لبرنامج الإعداد بإغلاق هذه التطبيقات تلقائيًا. بعد اكتمال التثبيت ، سيحاول برنامج الإعداد إعادة تشغيل التطبيقات.
CloseApplications=أغلق التطبيقات &تلقائيًا
DontCloseApplications=&لا تغلق التطبيقات
ErrorCloseApplications=لم يتمكن الإعداد من إغلاق جميع التطبيقات تلقائيًا. يوصى بإغلاق جميع التطبيقات التي تستخدم الملفات التي تحتاج إلى تحديث بواسطة برنامج الإعداد قبل المتابعة.
PrepareToInstallNeedsRestart=برنامج الإعداد يجب أن يقوم بإعادة تشغيل الجهاز. بعد إعادة تشغيل جهازك, قم بتشغيل برنامج الإعداد مرة أخرى لإكمال تثبيت [name].%n%nهل تحب إعادة التشغيل الآن?

; *** "Installing" wizard page
WizardInstalling=جاري التثبيت
InstallingLabel=يرجى الانتظار حتى يقوم برنامج الإعداد بتثبي [name] على جهازك.

; *** "Setup Completed" wizard page
FinishedHeadingLabel=إنهاء معالج تثبيت [name]
FinishedLabelNoIcons=إكتمل معالج التثبيت من تثبيت [name] على جهازك.
FinishedLabel=اكتمل معالج التثبيت من تثبيت [name] على جهازك. قد يتم تشغيل التطبيق عن طريق تحديد الاختصارات المثبتة.
ClickFinish=إضغط إنهاء للخروج من معالج التثبيت
FinishedRestartLabel=لاستكمال تثبيت [name], يجب على برنامج الإعداد إعادة تشغيل جهاز الكمبيوتر الخاص بك. هل ترغب في إعادة التشغيل الآن؟
FinishedRestartMessage=لاستكمال تثبيت [name], يجب على برنامج الإعداد إعادة تشغيل جهاز الكمبيوتر الخاص بك.%n%nهل ترغب في إعادة التشغيل الآن؟
ShowReadmeCheck=نعم ، أرغب عرض ملف README
YesRadio=&نعم أعد تشغيل الكومبيوتر الان
NoRadio=&لا ، سأعيد تشغيل الكمبيوتر لاحقًا
; used for example as 'Run MyProg.exe'
RunEntryExec=تشغيل %1
; used for example as 'View Readme.txt'
RunEntryShellExec=عرض %1

; *** "Setup Needs the Next Disk" stuff
ChangeDiskTitle=يحتاج برنامج الإعداد إلى القرص التالي
SelectDiskLabel2=الرجاء إدراج القرص %1 وانقر فوق حسنا.%n%nإذا كان يمكن العثور على الملفات الموجودة على هذا القرص في مجلد غير الذي يظهر أدناه، أدخل المسار الصحيح أو انقر فوق استعراض.
PathLabel=&مسار :
FileNotInDir2=لم نتمكن من العثور على الملف "%1" في "%2". الرجاء إدراج القرص الصحيح أو تحديد مجلد آخر.
SelectDirectoryLabel=الرجاء تحديد موقع القرص التالي.

; *** Installation phase messages
SetupAborted=لم يتم إكمال الإعداد. %n%nالرجاء تصحيح المشكلة وتشغيل الإعداد مرة أخرى.
AbortRetryIgnoreSelectAction=حدد إجراء
AbortRetryIgnoreRetry=أ&عد مجددا
AbortRetryIgnoreIgnore=&تجاهل الخطأ والمتابعة
AbortRetryIgnoreCancel=إلغاء التثبيت

; *** Installation status messages
StatusClosingApplications=إغلاق التطبيقات...
StatusCreateDirs=إنشاء المجلدات...
StatusExtractFiles=استخراج الملفات...
StatusCreateIcons=إنشاء الإختصارات...
StatusCreateIniEntries=إنشاء مدخلات INI...
StatusCreateRegistryEntries=إنشاء مفاتيح السجل...
StatusRegisterFiles=تسجيل الملفات...
StatusSavingUninstall=تسجيل معلومات إزالة التثبيت...
StatusRunProgram=الإنتهاء من التثبيت...
StatusRestartingApplications=إعادة تشغيل التطبيقات...
StatusRollback=التراجع عن التغييرات...

; *** Misc. errors
ErrorInternal2=خطأ داخلي: %1
ErrorFunctionFailedNoCode=فشل %1
ErrorFunctionFailed=فشل %1; رقم الخطء %2
ErrorFunctionFailedWithMessage=فشل %1; رقم الخطء %2.%n%3
ErrorExecutingProgram=الإعداد غير قابل على تشغيل الملف:%n%1

; *** Registry errors
ErrorRegOpenKey=خطأ في فتح مفتاح التسجيل:%n%1\%2
ErrorRegCreateKey=خطأ في إنشاء مفتاح التسجيل:%n%1\%2
ErrorRegWriteKey=خطأ في الكتابة على مفتاح التسجيل:%n%1\%2

; *** INI errors
ErrorIniEntry=حدث خطأ في إنشاء إدخال INI في الملف "%1".

; *** File copying errors
FileAbortRetryIgnoreSkipNotRecommended=&تخطي هذا الملف (غير مستحسن)
FileAbortRetryIgnoreIgnoreNotRecommended=&تجاهل الخطأ والمتابعة (غير مستحسن)
SourceIsCorrupted=الملف المصدر تالف
SourceDoesntExist=الملف "%1"غير موجود
ExistingFileReadOnly2=تعذر استبدال الملف الموجود لأنه تم وضع علامة للقراءة فقط.
ExistingFileReadOnlyRetry=&أزل القراءة فقط عن الملفات ثم حاول مرة أخرى
ExistingFileReadOnlyKeepExisting=&إحتفظ بالملفات الموجودة
ErrorReadingExistingDest=حدث خطأ أثناء محاولة قراءة الملف الموجود:
FileExistsSelectAction=اختر إجراء
FileExists2=الملف موجود بالفعل.
FileExistsOverwriteExisting=&استبدال الملف الموجود
FileExistsKeepExisting=ا&بقاء الملف الموجود
FileExistsOverwriteOrKeepAll=ا&فعل هذا للنزاعات القادمة
ExistingFileNewerSelectAction=اختر إجراء
ExistingFileNewer2=الملف الموجود أحدث من الملف الذي سيقوم معالج الإعداد بتثبيته.
ExistingFileNewerOverwriteExisting=&&استبدال الملف الموجود
ExistingFileNewerKeepExisting=ال&ابقاء على الملف الموجود (مستحسن)
ExistingFileNewerOverwriteOrKeepAll=ا&فعل هذا مع النزاعات القادمة
ErrorChangingAttr=حدث خطأ أثناء محاولة تغيير سمات الملف الموجود:
ErrorCreatingTemp=حدث خطأ أثناء محاولة إنشاء ملف في الدليل الوجهة:
ErrorReadingSource=حدث خطأ أثناء محاولة قراءة ملف مصدر:
ErrorCopying=حدث خطأ أثناء محاولة نسخ ملف:
ErrorReplacingExistingFile=حدث خطأ أثناء محاولة استبدال الملف الموجود:
ErrorRestartReplace=فشل إعادة تشغيل "استبدال":
ErrorRenamingTemp=حدث خطأ أثناء محاولة إعادة تسمية ملف في الدليل الوجهة:
ErrorRegisterServer=تعذر تسجيل ملفات DLL/OCX: %1
ErrorRegSvr32Failed=فشل RegSvr32 مع رمز الخروج %1
ErrorRegisterTypeLib=الإعداد غير قادر على تسجيل مكتبة النوع: %1

; *** Uninstall display name markings
; used for example as 'My Program (32-bit)'
UninstallDisplayNameMark=%1 (%2)
; used for example as 'My Program (32-bit, All users)'
UninstallDisplayNameMarks=%1 (%2, %3)
UninstallDisplayNameMark32Bit=32-bit
UninstallDisplayNameMark64Bit=64-bit
UninstallDisplayNameMarkAllUsers=كافة المستخدمين
UninstallDisplayNameMarkCurrentUser=المستخدم الحالي

; *** Post-installation errors
ErrorOpeningReadme=حدث خطأ أثناء محاولة فتح ملف إقرأني.
ErrorRestartingComputer=لم يتمكن برنامج الإعداد من إعادة تشغيل الكمبيوتر. الرجاء القيام بذلك يدوياً.

; *** Uninstaller messages
UninstallNotFound=الملف "%1" غير موجود. لا يمكن إزالة التثبيت.
UninstallOpenError=تعذر فتح "%1". لا يمكن إزالة التثبيت.
UninstallUnsupportedVer=ملف سجل الإزالة "%1" في تنسيق غير معروف من قبل هذا الإصدار من برنامج إلغاء التثبيت. لا يمكن إزالة التثبيت
UninstallUnknownEntry=إدخال غير معروف (%1) تمت مصادفة في سجل إلغاء التثبيت
ConfirmUninstall=هل أنت متأكد من أنك تريد إزالة %1 تماما, وجميع مكوناته?
UninstallOnlyOnWin64=يمكن إلغاء تثبيت هذا التثبيت على Windows 64-بت فقط.
OnlyAdminCanUninstall=يمكن إلغاء تثبيت هذا التثبيت فقط من قبل مستخدم له امتيازات إدارية.
UninstallStatusLabel=يرجى الإنتظار ريت ما يتم إزالة تثبيت %1 من جهازك.
UninstalledAll=تم إزالة %1 تماما من جهازك بنجاح.
UninstalledMost=اكتمل إزالة %1.%n%nتعذر إزالة بعض العناصر. يمكن إزالة هذه يدوياً.
UninstalledAndNeedsRestart=لإكمال إلغاء تثبيت %1, يجب إعادة تشغيل الكمبيوتر.%n%nهل تريد إعادة تشغيل الآن؟
UninstallDataCorrupted=الملف "%1" تالف. لا يمكن إزالة التثبيت

; *** Uninstallation phase messages
ConfirmDeleteSharedFileTitle=إزالة ملف مشترك؟
ConfirmDeleteSharedFile2=يشير النظام إلى أن الملف المشترك التالي لم يعد في الاستخدام من قبل أي برامج. هل ترغب في أن يقوم إلغاء التثبيت بإزالة هذا الملف المشترك?%n%nإذا كانت أية برامج لا تزال تستخدم هذا الملف وتتم إزالته، قد لا تعمل هذه البرامج بشكل صحيح. إذا كنت غير متأكد، اختر لا. ترك الملف على النظام الخاص بك لن يسبب أي ضرر.
SharedFileNameLabel=اسم الملف:
SharedFileLocationLabel=الموقع :
WizardUninstalling=حالة إزالة التثبيت
StatusUninstalling=جاري إزالة تثبيت %1...

; *** Shutdown block reasons
ShutdownBlockReasonInstallingApp=جاري تثبيت %1.
ShutdownBlockReasonUninstallingApp=جاري تثبيت %1.

; The custom messages below aren't used by Setup itself, but if you make
; use of them in your scripts, you'll want to translate them.

[CustomMessages]

NameAndVersion=%1 الإصدار %2
AdditionalIcons=اختصارات إضافية:
CreateDesktopIcon=إنشاء اختصار في &سطح المكتب
CreateQuickLaunchIcon=إنشاء اختصار "الت&شغيل السريع"
ProgramOnTheWeb=%1 على الأنترنت
UninstallProgram=إزالة تثبيت %1
LaunchProgram=تشغيل %1
AssocFileExtension=اربط %1 مع صيغة ملف %2
AssocingFileExtension=جاري ربط %1 مع صيغة ملف %2
AutoStartProgramGroupDescription=بدأ التشغيل:
AutoStartProgram=تشغيل %1 تلقائيا
AddonHostProgramNotFound= تعذر العثور على %1 في الموقع الذي إخترته.%n%nهل تريد المتابعة على أية حال؟
