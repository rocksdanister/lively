; *** Inno Setup version 6.1.0+ Lithuanian messages ***
;
; To download user-contributed translations of this file, go to:
;   https://jrsoftware.org/files/istrans/
;
; Note: When translating this text, do not add periods (.) to the end of
; messages that didn't have them already, because on those messages Inno
; Setup adds the periods automatically (appending a period would result in
; two periods being displayed).
; Translated by Robertas Rimas (Loptar AT takas DOT lt)
; Corrected and updated by Rolandas Rudomanskis (rolandasr AT gmail DOT com)
; Corrected and updated to version 6.0.3+ by Dalius Guzauskas (aka Tichij) (tichij AT mail DOT com)

[LangOptions]
; The following three entries are very important. Be sure to read and 
; understand the '[LangOptions] section' topic in the help file.
LanguageName=Lietuvi<0173>
LanguageID=$0427
LanguageCodePage=1257
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
SetupAppTitle=Diegimas
SetupWindowTitle=Diegimas - %1
UninstallAppTitle=Paðalinimas
UninstallAppFullTitle=„%1“ paðalinimas

; *** Misc. common
InformationTitle=Informacija
ConfirmTitle=Patvirtinimas
ErrorTitle=Klaida

; *** SetupLdr messages
SetupLdrStartupMessage=%1 diegimas. Norite tæsti?
LdrCannotCreateTemp=Negaliu sukurti laikinojo failo. Diegimas nutraukiamas
LdrCannotExecTemp=Negaliu ávykdyti failo laikinajame kataloge. Diegimas nutraukiamas
HelpTextNote=

; *** Startup error messages
LastErrorMessage=%1.%n%nKlaida %2: %3
SetupFileMissing=Diegimo kataloge nerastas „%1“ failas. Paðalinkite ðià problemà arba ásigykite naujà programos kopijà.
SetupFileCorrupt=Ádiegiami failai sugadinti. Ásigykite naujà programos kopijà.
SetupFileCorruptOrWrongVer=Ádiegiami failai yra sugadinti arba nesuderinami su diegimo programa. Iðtaisykite problemà arba ásigykite naujà programos kopijà.
InvalidParameter=Klaidingas parametras buvo gautas ið komandinës eilutës:%n%n%1
SetupAlreadyRunning=Diegimo programa jau yra paleista.
WindowsVersionNotSupported=Ði programa nesuderinama su Jûsø kompiuteryje ádiegta Windows versija.
WindowsServicePackRequired=Ði programa reikalauja %1 Service Pack %2 ar vëlesnës versijos.
NotOnThisPlatform=Ði programa negali bûti paleista %1 aplinkoje.
OnlyOnThisPlatform=Ði programa turi bûti leidþiama %1 aplinkoje.
OnlyOnTheseArchitectures=Ði programa gali bûti ádiegta tik Windows versijose, turinèiose ðias procesoriaus architektûras:%n%n%1
WinVersionTooLowError=Ði programa reikalauja %1 %2 ar vëlesnës versijos.
WinVersionTooHighError=Ði programa negali bûti ádiegta %1 %2 ar vëlesnës versijos aplinkoje.
AdminPrivilegesRequired=Ðios programos diegimui privalote bûti prisijungæs Administratoriaus teisëmis.
PowerUserPrivilegesRequired=Ðios programos diegimui privalote bûti prisijungæs Administratoriaus arba „Power Users“ grupës nario teisëmis.
SetupAppRunningError=Diegimo programa aptiko, kad yra paleista „%1“.%n%nUþdarykite visas paleistas ðios programos kopijas ir, jei norite tæsti, paspauskite „Gerai“ arba „Atðaukti“, jei norite nutraukti diegimà.
UninstallAppRunningError=Paðalinimo programa aptiko, kad yra paleista „%1“.%n%nUþdarykite visas paleistas ðios programos kopijas ir, jei norite tæsti, paspauskite „Gerai“ arba „Atðaukti“, jei norite nutraukti diegimà.

; *** Startup questions
PrivilegesRequiredOverrideTitle=Diegimo reþimo pasirinkimas
PrivilegesRequiredOverrideInstruction=Pasirinkite diegimo reþimà
PrivilegesRequiredOverrideText1=%1 gali bûti ádiegta visiems naudotojams (reikalingos administratoriaus teisës) arba tik jums.
PrivilegesRequiredOverrideText2=%1 gali bûti ádiegta arba tik jums arba visiems naudotojams (reikalingos administratoriaus teisës).
PrivilegesRequiredOverrideAllUsers=Ádiegti &visiems naudotojams
PrivilegesRequiredOverrideAllUsersRecommended=Ádiegti &visiems naudotojams (rekomenduojama)
PrivilegesRequiredOverrideCurrentUser=Ádiegti tik &man
PrivilegesRequiredOverrideCurrentUserRecommended=Ádiegti tik &man (rekomenduojama)

; *** Misc. errors
ErrorCreatingDir=Diegimo programa negali sukurti katalogo „%1“
ErrorTooManyFilesInDir=Neámanoma sukurti failo „%1“ kataloge, nes jame per daug failø

; *** Setup common messages
ExitSetupTitle=Uþdaryti diegimo programà
ExitSetupMessage=Diegimas nebaigtas. Jei baigsite dabar, programa nebus ádiegta.%n%nJûs galite paleisti diegimo programà kità kartà, kad pabaigtumëte diegimà.%n%nUþdaryti diegimo programà?
AboutSetupMenuItem=&Apie diegimo programà...
AboutSetupTitle=Apie diegimo programà
AboutSetupMessage=%1 versija %2%n%3%n%n%1 puslapis internete:%n%4
AboutSetupNote=
TranslatorNote=

; *** Buttons
ButtonBack=< &Atgal
ButtonNext=&Pirmyn >
ButtonInstall=Á&diegti
ButtonOK=Gerai
ButtonCancel=Atðaukti
ButtonYes=&Taip
ButtonYesToAll=Taip &viskà
ButtonNo=&Ne
ButtonNoToAll=N&e nieko
ButtonFinish=&Pabaiga
ButtonBrowse=&Nurodyti...
ButtonWizardBrowse=Nu&rodyti...
ButtonNewFolder=&Naujas katalogas

; *** "Select Language" dialog messages
SelectLanguageTitle=Pasirinkite diegimo programos kalbà
SelectLanguageLabel=Pasirinkite diegimo metu naudojamà kalbà.

; *** Common wizard text
ClickNext=Paspauskite „Pirmyn“, jei norite tæsti, arba „Atðaukti“, jei norite iðeiti ið diegimo programos.
BeveledLabel=
BrowseDialogTitle=Nurodykite katalogà
BrowseDialogLabel=Pasirinkite katalogà ið sàraðo ir paspauskite „Gerai“.
NewFolderName=Naujas katalogas

; *** "Welcome" wizard page
WelcomeLabel1=Sveiki! Èia „[name]“ diegimo programa.
WelcomeLabel2=Diegimo programa ádiegs „[name]“ Jûsø kompiuteryje.%n%nPrieð tæsiant diegimà, rekomenduojama uþdaryti visas nereikalingas programas.

; *** "Password" wizard page
WizardPassword=Slaptaþodis
PasswordLabel1=Ðis diegimas yra apsaugotas slaptaþodþiu.
PasswordLabel3=Áveskite slaptaþodá ir spauskite „Pirmyn“, jei norite tæsti diegimà. Atkreipkite dëmesá: didþiosios ir maþosios raidës vertinamos skirtingai (case sensitive).
PasswordEditLabel=&Slaptaþodis:
IncorrectPassword=Ávestas slaptaþodis yra neteisingas. Pabandykite ið naujo.

; *** "License Agreement" wizard page
WizardLicense=Licencinë sutartis
LicenseLabel=Perskaitykite ðià informacijà prieð tæsdami diegimà.
LicenseLabel3=Perskaitykite Licencijos sutartá. Prieð tæsdami diegimà Jûs turite sutikti su reikalavimais.
LicenseAccepted=Að &sutinku su reikalavimais
LicenseNotAccepted=Að &nesutinku su reikalavimais

; *** "Information" wizard pages
WizardInfoBefore=Informacija
InfoBeforeLabel=Perskaitykite ðià informacijà prieð tæsiant diegimà.
InfoBeforeClickLabel=Kai bûsite pasiruoðæs tæsti diegimà, spauskite „Pirmyn“.
WizardInfoAfter=Informacija
InfoAfterLabel=Perskaitykite ðià informacijà prieð tæsiant diegimà.
InfoAfterClickLabel=Spauskite „Pirmyn“, kai bûsite pasiruoðæ tæsti diegimà.

; *** "User Information" wizard page
WizardUserInfo=Informacija apie naudotojà
UserInfoDesc=Áveskite naudotojo duomenis.
UserInfoName=&Naudotojo vardas:
UserInfoOrg=&Organizacija:
UserInfoSerial=&Serijinis numeris:
UserInfoNameRequired=Jûs privalote ávesti vardà.

; *** "Select Destination Location" wizard page
WizardSelectDir=Pasirinkite diegimo katalogà
SelectDirDesc=Kur turi bûti ádiegta „[name]“?
SelectDirLabel3=Diegimo programa ádiegs „[name]“ á nurodytà katalogà.
SelectDirBrowseLabel=Norëdami tæsti diegimà spauskite „Pirmyn“. Jei norite pasirinkti kità katalogà, spauskite „Nurodyti“.
DiskSpaceGBLabel=Reikia maþiausiai [gb] GB laisvos vietos kietajame diske.
DiskSpaceMBLabel=Reikia maþiausiai [mb] MB laisvos vietos kietajame diske.
CannotInstallToNetworkDrive=Diegimo programa negali diegti á tinkliná diskà.
CannotInstallToUNCPath=Diegimo programa negali diegti á UNC tipo katalogà.
InvalidPath=Jûs privalote áraðyti pilnà kelià su disko raide; pavyzdþiui:%n%nC:\APP%n% ir negalima nurodyti UNC tipo katalogà:%n%n\\Serveris\share
InvalidDrive=Diskas, kurá nurodëte, neegzistuoja arba yra neprieinamas. Nurodykite kità diskà ir/arba katalogà.
DiskSpaceWarningTitle=Nepakanka laisvos vietos diske
DiskSpaceWarning=Diegimui reikia bent %1 KB laisvos vietos, bet nurodytame diske yra tik %2 KB laisvos vietos.%n%nVis tiek norite tæsti?
DirNameTooLong=Katalogo pavadinimas ar kelias iki jo per ilgas.
InvalidDirName=Nekorektiðkas katalogo pavadinimas.
BadDirName32=Katalogo pavadinime neturi bûti simboliø:%n%n%1
DirExistsTitle=Tokio katalogo nëra
DirExists=Katalogas:%n%n%1%n%n jau yra. Vis tiek norite diegti programà tame kataloge?
DirDoesntExistTitle=Tokio katalogo nëra.
DirDoesntExist=Katalogas:%n%n%1%n%n neegzistuoja. Norite kad katalogas bûtø sukurtas?

; *** "Select Components" wizard page
WizardSelectComponents=Komponentø pasirinkimas
SelectComponentsDesc=Kurie komponentai turi bûti ádiegti?
SelectComponentsLabel2=Paþymëkite komponentus, kuriuos norite ádiegti; nuimkite þymes nuo komponentø, kuriø nenorite diegti. Kai bûsite pasiruoðæs tæsti, spauskite „Pirmyn“.
FullInstallation=Pilnas visø komponentø diegimas
; if possible don't translate 'Compact' as 'Minimal' (I mean 'Minimal' in your language)
CompactInstallation=Glaustas diegimas
CustomInstallation=Pasirinktinis diegimas
NoUninstallWarningTitle=Komponentai egzistuoja
NoUninstallWarning=Diegimo programa aptiko, kad ðie komponentai jau ádiegti Jûsø kompiuteryje:%n%n%1%n%nJei nuimsite þymes nuo ðiø komponentø, jie vis tiek nebus iðtrinti.%n%nVis tiek norite tæsti diegimà?
ComponentSize1=%1 KB
ComponentSize2=%1 MB
ComponentsDiskSpaceGBLabel=Dabartinis Jûsø pasirinkimas reikalauja [gb] GB laisvos vietos diske.
ComponentsDiskSpaceMBLabel=Dabartinis Jûsø pasirinkimas reikalauja [mb] MB laisvos vietos diske.

; *** "Select Additional Tasks" wizard page
WizardSelectTasks=Nurodykite papildomus veiksmus
SelectTasksDesc=Kokius papildomus veiksmus reikia atlikti?
SelectTasksLabel2=Nurodykite papildomus veiksmus, kuriuos diegimo programa turës atlikti „[name]“ diegimo metu. Kai bûsite pasiruoðæs tæsti diegimà, spauskite „Pirmyn“.

; *** "Select Start Menu Folder" wizard page
WizardSelectProgramGroup=Nurodykite „Start Menu“ katalogà
SelectStartMenuFolderDesc=Kur diegimo programa turëtø sukurti nuorodas?
SelectStartMenuFolderLabel3=Nuorodos bus sukurtos ðiame „Start Menu“ kataloge.
SelectStartMenuFolderBrowseLabel=Norëdami tæsti diegimà spauskite „Pirmyn“. Jei norite parinkti kità katalogà, spauskite „Nurodyti“.
MustEnterGroupName=Jûs privalote ávesti katalogo pavadinimà.
GroupNameTooLong=Katalogo pavadinimas ar kelias iki jo per ilgas.
InvalidGroupName=Katalogo pavadinimas yra nekorektiðkas.
BadGroupName=Katalogo pavadinime neturi bûti simboliø:%n%n%1
NoProgramGroupCheck2=&Nekurti „Start Menu“ katalogo

; *** "Ready to Install" wizard page
WizardReady=Pasirengta diegimui
ReadyLabel1=Diegimo programa pasirengusi diegti „[name]“ Jûsø kompiuteryje.
ReadyLabel2a=Spauskite „Ádiegti“, jei norite tæsti diegimà, arba „Atgal“, jeigu norite perþiûrëti nustatymus arba juos pakeisti.
ReadyLabel2b=Spauskite „Ádiegti“, jei norite tæsti diegimà.
ReadyMemoUserInfo=Naudotojo informacija:
ReadyMemoDir=Katalogas diegimui:
ReadyMemoType=Diegimo tipas:
ReadyMemoComponents=Pasirinkti komponentai:
ReadyMemoGroup=„Start Menu“ katalogas:
ReadyMemoTasks=Papildomi veiksmai:

; *** TDownloadWizardPage wizard page and DownloadTemporaryFile
DownloadingLabel=Parsisiunèiami papildomi failai...
ButtonStopDownload=&Stabdyti parsisiuntimà
StopDownload=Ar tikrai norite sustabdyti parsisiuntimà?
ErrorDownloadAborted=Parsisiuntimas nutrauktas
ErrorDownloadFailed=Parsisiøsti nepavyko: %1 %2
ErrorDownloadSizeFailed=Nepavyko gauti dydþio: %1 %2
ErrorFileHash1=Failo patikrinimas nepavyko: %1
ErrorFileHash2=Neteisinga failo „hash“ reikðmë: numatyta %1, rasta %2
ErrorProgress=Netinkama eiga: %1 ið %2
ErrorFileSize=Neteisingas failo dydis: numatytas %1, rastas %2

; *** "Preparing to Install" wizard page
WizardPreparing=Pasirengimas diegimui
PreparingDesc=Diegimo programa pasirengusi „[name]“ diegimui Jûsø kompiuteryje.
PreviousInstallNotCompleted=Ankstesnës programos diegimas/ðalinimas buvo neuþbaigtas. Jums reikëtø perkrauti kompiuterá, kad uþbaigtumëte diegimà.%n%nKai perkrausite kompiuterá, paleiskite diegimo programà dar kartà, kad pabaigtumëte „[name]“ diegimà.
CannotContinue=Diegimas negali bûti tæsiamas. Paspauskite „Atðaukti“ diegimo uþbaigimui.
ApplicationsFound=Ðios programos naudoja failus, kurie turi bûti perraðyti diegimo metu. Rekomenduojama leisti diegimo programai automatiðkai uþdaryti ðias programas.
ApplicationsFound2=Ðios programos naudoja failus, kurie turi bûti perraðyti diegimo metu. Rekomenduojama leisti diegimo programai automatiðkai uþdaryti ðias programas. Po to, kai diegimas bus baigtas, diegimo programa bandys ið naujo paleisti ðias programas.
CloseApplications=&Automatiðkai uþdaryti programas
DontCloseApplications=&Neuþdarinëti programø
ErrorCloseApplications=Diegimo programai nepavyko automatiðkai uþdaryti visø programø. Prieð tæsiant diegimà, rekomeduojama uþdaryti visas programas, naudojanèias failus, kurie turi bûti perraðyti diegimo metu.
PrepareToInstallNeedsRestart=Diegimo programai reikia perkrauti kompiuterá. Po perkovimo, vël paleiskite diegimo programà „[name]“ diegimo uþbaigimui.%n%nNorite perkrauti já dabar?

; *** "Installing" wizard page
WizardInstalling=Vyksta diegimas
InstallingLabel=Palaukite kol diegimo programa ádiegs „[name]“ Jûsø kompiuteryje.

; *** "Setup Completed" wizard page
FinishedHeadingLabel=„[name]“ diegimas baigtas
FinishedLabelNoIcons=Diegimo programa baigë „[name]“ diegimà Jûsø kompiuteryje.
FinishedLabel=Diegimo programa baigë „[name]“ diegimà Jûsø kompiuteryje. Programa gali bûti paleista pasirinkus atitinkamas nuorodas.
ClickFinish=Spauskite „Pabaiga“, kad uþdarytumëte diegimo programà.
FinishedRestartLabel=„[name]“ diegimo uþbaigimui, reikia perkrauti kompiuterá. Norite perkrauti já dabar?
FinishedRestartMessage=„[name]“ diegimo uþbaigimui, reikia perkrauti kompiuterá.%n%nNorite perkrauti já dabar?
ShowReadmeCheck=Taip, að norëèiau perskaityti „README“ failà
YesRadio=&Taip, að noriu perkrauti kompiuterá dabar
NoRadio=&Ne, að perkrausiu kompiuterá vëliau
; used for example as 'Run MyProg.exe'
RunEntryExec=Vykdyti „%1“
; used for example as 'View Readme.txt'
RunEntryShellExec=Perþiûrëti „%1“

; *** "Setup Needs the Next Disk" stuff
ChangeDiskTitle=Diegimo programai reikia kito diskelio
SelectDiskLabel2=Idëkite diskelá %1 ir spauskite „Gerai“.%n%nJeigu reikiami failai gali bûti rasti kitame kataloge, nei pavaizduota þemiau, áveskite teisingà kelià arba spauskite „Nurodyti“.
PathLabel=&Katalogas:
FileNotInDir2=„%1“ failas nerastas „%2“ kataloge. Ádëkite teisingà diskelá arba nurodykite teisingà kelià.
SelectDirectoryLabel=Nurodykite kito diskelio vietà.

; *** Installation phase messages
SetupAborted=Diegimas nebuvo baigtas.%n%nPaðalinkite prieþàstá ir pakartokite diegimà vël.
AbortRetryIgnoreSelectAction=Pasirinkite veiksmà
AbortRetryIgnoreRetry=Pabandyti dar kar&tà
AbortRetryIgnoreIgnore=&Ignoruoti klaidà ir tæsti
AbortRetryIgnoreCancel=Nutraukti diegimà

; *** Installation status messages
StatusClosingApplications=Uþdaromos programos...
StatusCreateDirs=Kuriami katalogai...
StatusExtractFiles=Iðpakuojami failai...
StatusCreateIcons=Kuriamos nuorodos...
StatusCreateIniEntries=Kuriami INI áraðai...
StatusCreateRegistryEntries=Kuriami registro áraðai...
StatusRegisterFiles=Registruojami failai...
StatusSavingUninstall=Iðsaugoma informacija programos paðalinimui...
StatusRunProgram=Baigiamas diegimas...
StatusRestartingApplications=Ið naujo paleidþiamos programos...
StatusRollback=Anuliuojami pakeitimai...

; *** Misc. errors
ErrorInternal2=Vidinë klaida: %1
ErrorFunctionFailedNoCode=%1 nepavyko
ErrorFunctionFailed=%1 nepavyko; kodas %2
ErrorFunctionFailedWithMessage=%1 nepavyko; kodas %2.%n%3
ErrorExecutingProgram=Nepavyko paleisti failo:%n%1

; *** Registry errors
ErrorRegOpenKey=Klaida skaitant registro áraðà:%n%1\%2
ErrorRegCreateKey=Klaida sukuriant registro áraðà:%n%1\%2
ErrorRegWriteKey=Klaida raðant registro áraðà:%n%1\%2

; *** INI errors
ErrorIniEntry=Klaida raðant INI áraðà „%1“ faile.

; *** File copying errors
FileAbortRetryIgnoreSkipNotRecommended=Pralei&sti ðá failà (nerekomenduojama)
FileAbortRetryIgnoreIgnoreNotRecommended=&Ignoruoti klaidà ir tæsti (nerekomenduojama)
SourceIsCorrupted=Pradinis failas sugadintas
SourceDoesntExist=Pradinio failo „%1“ nëra
ExistingFileReadOnly2=Esamas failas yra paþymëtas „Tik skaitymui“ todël negali bûti pakeistas.
ExistingFileReadOnlyRetry=Paðalinkite at&ributà „Tik skaitymui“ ir bandykite vël
ExistingFileReadOnlyKeepExisting=Pali&kti esamà failà
ErrorReadingExistingDest=Skaitant esamà failà ávyko klaida:
FileExistsSelectAction=Pasirinkite veiksmà
FileExists2=Toks failas jau yra.
FileExistsOverwriteExisting=&Perraðyti esamà failà
FileExistsKeepExisting=Pali&kti esamà failà
FileExistsOverwriteOrKeepAll=&Daryti taip ir esant kitiems konfliktams
ExistingFileNewerSelectAction=Pasirinkite veiksmà
ExistingFileNewer=Esamas failas yra naujesnis uþ tà, kurá diegimo programa bando áraðyti. Rekomenduojama palikti esamà failà.%n%nPalikti naujesná failà?
ExistingFileNewer2=Esamas failas yra naujesnis uþ tà, kurá diegimo programa bando áraðyti.
ExistingFileNewerOverwriteExisting=&Perraðyti esamà failà
ExistingFileNewerKeepExisting=Pali&kti esamà failà (rekomenduojama)
ExistingFileNewerOverwriteOrKeepAll=&Daryti taip ir esant kitiems konfliktams
ErrorChangingAttr=Keièiant failo atributus ávyko klaida:
ErrorCreatingTemp=Kuriant failà pasirinktame kataloge ávyko klaida:
ErrorReadingSource=Skaitant diegiamàjá failà ávyko klaida:
ErrorCopying=Kopijuojant failà ávyko klaida:
ErrorReplacingExistingFile=Perraðant esamà failà ávyko klaida:
ErrorRestartReplace=Perkrovimas/Perraðymas nepavyko:
ErrorRenamingTemp=Pervadinant failà pasirinktame kataloge ávyko klaida:
ErrorRegisterServer=Nepavyko uþregistruoti DLL/OCX bibliotekos: „%1“
ErrorRegSvr32Failed=RegSvr32 registracijos klaida %1
ErrorRegisterTypeLib=Nepavyko uþregistruoti tipø bibliotekos: „%1“

; *** Uninstall display name markings
; used for example as 'My Program (32-bit)'
UninstallDisplayNameMark=%1 (%2)
; used for example as 'My Program (32-bit, All users)'
UninstallDisplayNameMarks=%1 (%2, %3)
UninstallDisplayNameMark32Bit=32-bitø
UninstallDisplayNameMark64Bit=64-bitø
UninstallDisplayNameMarkAllUsers=Visiems naudotojams
UninstallDisplayNameMarkCurrentUser=Esamam naudotojui

; *** Post-installation errors
ErrorOpeningReadme=Bandant atidaryti „README“ failà ávyko klaida.
ErrorRestartingComputer=Diegimo programa negali perkrauti kompiuterio. Perkraukite kompiuterá áprastu bûdu.

; *** Uninstaller messages
UninstallNotFound=„%1“ failo nëra. Paðalinti neámanoma.
UninstallOpenError=„%1“ failas negali bûti atidarytas. Paðalinti neámanoma.
UninstallUnsupportedVer=Paðalinimo þurnalo failas „%1“ yra paðalinimo programai nesuprantamo formato. Paðalinti neámanoma.
UninstallUnknownEntry=Neþinomas áraðas (%1) rastas paðalinimo þurnalo faile.
ConfirmUninstall=Esate tikri, kad norite paðalinti „%1“ ir visus priklausanèius komponentus?
UninstallOnlyOnWin64=Ðis diegimas gali bûti paðalintas tik 64 bitø Windows sistemose.
OnlyAdminCanUninstall=Tik administratoriaus teises turintis naudotojas gali paðalinti programà.
UninstallStatusLabel=Palaukite, kol „%1“ bus paðalinta ið Jûsø kompiuterio.
UninstalledAll=„%1“ buvo sëkmingai paðalinta ið Jûsø kompiuterio.
UninstalledMost=„%1“ paðalinimas sëkmingai baigtas.%n%nKai kurie elementai nebuvo iðtrinti - juos galite paðalinti rankiniu bûdu.
UninstalledAndNeedsRestart=„%1“ paðalinimui uþbaigti Jûsø kompiuteris turi bûti perkrautas.%n%nNorite perkrauti já dabar?
UninstallDataCorrupted=„%1“ failas yra sugadintas. Programos paðalinti neámanoma.

; *** Uninstallation phase messages
ConfirmDeleteSharedFileTitle=Iðtrinti bendruosius failus?
ConfirmDeleteSharedFile2=Aptikta, kad jokia programa nenaudoja bendrøjø failø. Norite iðtrinti bendruosius failus? %n%nJeigu kurios nors programos naudoja ðiuos failus, ir jie bus iðtrinti, tos programos gali veikti neteisingai. Jeigu nesate tikras - spauskite „Ne“. Failo palikimas Jûsø kompiuteryje nesukels jokiø problemø.
SharedFileNameLabel=Failo pavadinimas:
SharedFileLocationLabel=Vieta:
WizardUninstalling=Paðalinimo eiga
StatusUninstalling=Ðalinama „%1“...

; *** Shutdown block reasons
ShutdownBlockReasonInstallingApp=Diegiama „%1“.
ShutdownBlockReasonUninstallingApp=Ðalinama „%1“.

; The custom messages below aren't used by Setup itself, but if you make
; use of them in your scripts, you'll want to translate them.

[CustomMessages]

NameAndVersion=%1 versija %2
AdditionalIcons=Papildomos nuorodos:
CreateDesktopIcon=Sukurti nuorodà &Darbalaukyje
CreateQuickLaunchIcon=Sukurti Sparèiosios &Paleisties nuorodà
ProgramOnTheWeb=„%1“ þiniatinklyje
UninstallProgram=Paðalinti „%1“
LaunchProgram=Paleisti „%1“
AssocFileExtension=&Susieti „%1“ programà su failo plëtiniu %2
AssocingFileExtension=„%1“ programa susiejama su failo plëtiniu %2...
AutoStartProgramGroupDescription=Automatinë paleistis:
AutoStartProgram=Automatiðkai paleisti „%1“
AddonHostProgramNotFound=„%1“ nerasta Jûsø nurodytame kataloge.%n%nVis tiek norite tæsti?
