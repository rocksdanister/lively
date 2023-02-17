; *** Inno Setup version 6.1.0+ Greek messages ***
;
; To download user-contributed translations of this file, go to:
;   https://jrsoftware.org/files/istrans/
;
; Note: When translating this text, do not add periods (.) to the end of
; messages that didn't have them already, because on those messages Inno
; Setup adds the periods automatically (appending a period would result in
; two periods being displayed).
;
; Originally translated by Anastasis Chatzioglou, baldycom@hotmail.com
; Updated by XhmikosR [XhmikosR, my_nickname at yahoo dot com]
; Updated to version 6.1.0+ by Vasileios Karamichail, v.karamichail@outlook.com
;

[LangOptions]
; The following three entries are very important. Be sure to read and 
; understand the '[LangOptions] section' topic in the help file.
LanguageName=Ελληνικά
LanguageID=$0408
LanguageCodePage=1253
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
SetupAppTitle=Εγκατάσταση
SetupWindowTitle=Εγκατάσταση - %1
UninstallAppTitle=Απεγκατάσταση
UninstallAppFullTitle=%1 Απεγκατάσταση

; *** Misc. common
InformationTitle=Πληροφορίες
ConfirmTitle=Επιβεβαίωση
ErrorTitle=Σφάλμα

; *** SetupLdr messages
SetupLdrStartupMessage=Θα εκτελεστεί η εγκατάσταση του %1. Θέλετε να συνεχίσετε;
LdrCannotCreateTemp=Σφάλμα στη δημιουργία προσωρινού αρχείου. Η εγκατάσταση τερματίστηκε
LdrCannotExecTemp=Αδύνατη η εκτέλεση αρχείου στον φάκελο προσωρινών αρχείων. Η εγκατάσταση τερματίστηκε
HelpTextNote=

; *** Startup error messages
LastErrorMessage=%1.%n%nΣφάλμα %2: %3
SetupFileMissing=Το αρχείο %1 λείπει από τον κατάλογο εγκατάστασης. Διορθώστε το πρόβλημα ή αποκτήστε ένα νέο αντίγραφο του προγράμματος.
SetupFileCorrupt=Το αρχείο εγκατάστασης είναι κατεστραμμένο. Παρακαλώ προμηθευτείτε ένα νέο αντίγραφο του προγράμματος.
SetupFileCorruptOrWrongVer=Το αρχείο εγκατάστασης είναι κατεστραμμένο ή δεν είναι συμβατό με αυτήν την έκδοση του προγράμματος εγκατάστασης. Διορθώστε το πρόβλημα ή αποκτήστε ένα νέο αντίγραφο του προγράμματος.
InvalidParameter=Μία μη έγκυρη παράμετρος χρησιμοποιήθηκε στη γραμμή εντολών:%n%n%1
SetupAlreadyRunning=Η εγκατάσταση τρέχει ήδη.
WindowsVersionNotSupported=Αυτό το πρόγραμμα δεν υποστηρίζει την έκδοση των Windows που εκτελεί ο υπολογιστής σας.
WindowsServicePackRequired=Αυτό το πρόγραμμα χρειάζεται το %1 Service Pack %2 ή νεότερο.
NotOnThisPlatform=Αυτό το πρόγραμμα δεν μπορεί να εκτελεστεί σε %1.
OnlyOnThisPlatform=Αυτό το πρόγραμμα μπορεί να εκτελεστεί μόνο σε %1.
OnlyOnTheseArchitectures=Αυτό το πρόγραμμα μπορεί να εγκατασταθεί μόνο σε εκδόσεις των Windows που έχουν σχεδιαστεί για τις ακόλουθες αρχιτεκτονικές επεξεργαστών:%n%n%1
WinVersionTooLowError=Αυτό το πρόγραμμα απαιτεί %1 έκδοση %2 ή μεταγενέστερη.
WinVersionTooHighError=Αυτό το πρόγραμμα δεν μπορεί να εγκατασταθεί σε %1 έκδοση %2 ή μεταγενέστερη.
AdminPrivilegesRequired=Πρέπει να είστε συνδεδεμένοι ως διαχειριστής κατά την εγκατάσταση αυτού του προγράμματος.
PowerUserPrivilegesRequired=Πρέπει να είστε συνδεδεμένοι ως διαχειριστής ή ως μέλος της ομάδας Power User κατά την εγκατάσταση αυτού του προγράμματος.
SetupAppRunningError=Ο Οδηγός Εγκατάστασης εντόπισε ότι η εφαρμογή %1 εκτελείται ήδη.%n%nΠαρακαλώ κλείστε την εφαρμογή τώρα και πατήστε ΟΚ για να συνεχίσετε, ή Άκυρο για έξοδο.
UninstallAppRunningError=Ο Οδηγός Απεγκατάστασης εντόπισε ότι η εφαρμογή %1 εκτελείται ήδη.%n%nΠαρακαλώ κλείστε την εφαρμογή τώρα και πατήστε ΟΚ για να συνεχίσετε, ή Άκυρο για έξοδο.

; *** Startup questions
PrivilegesRequiredOverrideTitle=Επιλέξτε Τρόπο Εγκατάστασης
PrivilegesRequiredOverrideInstruction=Επιλέξτε τον τρόπο εγκατάστασης
PrivilegesRequiredOverrideText1=Το %1 μπορεί να εγκατασταθεί για όλους τους χρήστες (απαιτεί δικαιώματα διαχειριστή) ή μόνο για εσάς.
PrivilegesRequiredOverrideText2=Το %1 μπορεί να εγκατασταθεί μόνο για εσάς ή για όλους τους χρήστες (απαιτεί δικαιώματα διαχειριστή).
PrivilegesRequiredOverrideAllUsers=Εγκατάσταση για &όλους τους χρήστες
PrivilegesRequiredOverrideAllUsersRecommended=Εγκατάσταση για όλ&ους τους χρήστες (συνιστάται)
PrivilegesRequiredOverrideCurrentUser=Εγκατάσταση μόνο για &εμένα
PrivilegesRequiredOverrideCurrentUserRecommended=Εγκατάσταση μόνο για &εμένα (συνιστάται)

; *** Misc. errors
ErrorCreatingDir=Η εγκατάσταση δεν μπόρεσε να δημιουργήσει τον φάκελο "%1"
ErrorTooManyFilesInDir=Δεν είναι δυνατή η δημιουργία ενός αρχείου στον φάκελο "%1" επειδή περιέχει πολλά αρχεία

; *** Setup common messages
ExitSetupTitle=Τέλος Εγκατάστασης
ExitSetupMessage=Η εγκατάσταση δεν έχει ολοκληρωθεί. Αν την τερματίσετε τώρα, το πρόγραμμα δεν θα εγκατασταθεί.%n%nΜπορείτε να εκτελέσετε ξανά την εγκατάσταση αργότερα.%n%nΈξοδος;
AboutSetupMenuItem=&Σχετικά με την Εγκατάσταση...
AboutSetupTitle=Σχετικά με την Εγκατάσταση
AboutSetupMessage=%1 έκδοση %2%n%3%n%n%1 αρχική σελίδα:%n%4
AboutSetupNote=
TranslatorNote=

; *** Buttons
ButtonBack=< &Πίσω
ButtonNext=&Επόμενο >
ButtonInstall=&Εγκατάσταση
ButtonOK=ΟΚ
ButtonCancel=&Ακυρο
ButtonYes=Ν&αι
ButtonYesToAll=Ναι σε &Ολα
ButtonNo=Ό&χι
ButtonNoToAll=Όχι &σε όλα
ButtonFinish=&Τέλος
ButtonBrowse=&Αναζήτηση...
ButtonWizardBrowse=Ανα&ζήτηση...
ButtonNewFolder=&Δημιουργία νέου φακέλου

; *** "Select Language" dialog messages
SelectLanguageTitle=Επιλογή Γλώσσας Οδηγού Εγκατάστασης
SelectLanguageLabel=Επιλέξτε τη γλώσσα που θέλετε να χρησιμοποιήσετε κατά την εγκατάσταση.

; *** Common wizard text
ClickNext=Πατήστε Επόμενο για να συνεχίσετε ή Άκυρο για να τερματίσετε την εγκατάσταση.
BeveledLabel=
BrowseDialogTitle=Αναζήτηση Φακέλου
BrowseDialogLabel=Επιλέξτε ένα φάκελο από την ακόλουθη λίστα και πατήστε ΟΚ.
NewFolderName=Νέος φάκελος

; *** "Welcome" wizard page
WelcomeLabel1=Καλως ορίσατε στον Οδηγό Εγκατάστασης του [name]
WelcomeLabel2=Θα γίνει εγκατάσταση του [name/ver] στον υπολογιστή σας.%n%nΣυνιστάται να κλείσετε όλες τις άλλες εφαρμογές πριν συνεχίσετε.

; *** "Password" wizard page
WizardPassword=Κωδικός Πρόσβασης
PasswordLabel1=Αυτή η εγκατάσταση προστατεύεται με κωδικό πρόσβασης.
PasswordLabel3=Παρακαλώ εισάγετε τον κωδικό και πατήστε Επόμενο.
PasswordEditLabel=&Κωδικός:
IncorrectPassword=Ο κωδικός που έχετε εισάγει είναι λανθασμένος. Παρακαλώ, προσπαθήστε ξανά.

; *** "License Agreement" wizard page
WizardLicense=Άδεια Χρήσης
LicenseLabel=Παρακαλώ διαβάστε προσεκτικά τις ακόλουθες πληροφορίες πριν συνεχίσετε.
LicenseLabel3=Παρακαλώ διαβάστε την ακόλουθη Άδεια Χρήσης. Θα πρέπει να αποδεχτείτε τους όρους της πριν συνεχίσετε την εγκατάσταση.
LicenseAccepted=&Δέχομαι τους όρους της Άδειας Χρήσης
LicenseNotAccepted=Δεν &αποδέχομαι τους όρους της Άδειας Χρήσης

; *** "Information" wizard pages
WizardInfoBefore=Πληροφορίες
InfoBeforeLabel=Παρακαλώ διαβάστε προσεκτικά τις ακόλουθες πληροφορίες πριν συνεχίσετε.
InfoBeforeClickLabel=Όταν είστε έτοιμοι να συνεχίσετε με τον Οδηγό Εγκατάστασης, πατήστε Επόμενο.
WizardInfoAfter=Πληροφορίες
InfoAfterLabel=Παρακαλώ διαβάστε προσεκτικά τις ακόλουθες πληροφορίες πριν συνεχίσετε.
InfoAfterClickLabel=Όταν είστε έτοιμοι να συνεχίσετε με τον Οδηγό Εγκατάστασης, πατήστε Επόμενο.

; *** "User Information" wizard page
WizardUserInfo=Πληροφορίες Χρήστη
UserInfoDesc=Παρακαλώ εισάγετε τα στοιχεία σας.
UserInfoName=&Ονομα Χρήστη:
UserInfoOrg=&Εταιρεία:
UserInfoSerial=&Σειριακός Αριθμός:
UserInfoNameRequired=Πρέπει να εισάγετε ένα όνομα.

; *** "Select Destination Location" wizard page
WizardSelectDir=Επιλογή Φακέλου Εγκατάστασης
SelectDirDesc=Πού θέλετε να εγκατασταθεί το [name];
SelectDirLabel3=Ο Οδηγός Εγκατάστασης θα εγκαταστήσει το [name] στον ακόλουθο φάκελο.
SelectDirBrowseLabel=Για να συνεχίσετε, πατήστε Επόμενο. Εάν θέλετε να επιλέξετε διαφορετικό φάκελο, πατήστε Αναζήτηση.
DiskSpaceGBLabel=Απαιτούνται τουλάχιστον [gb] GB ελεύθερου χώρου στο δίσκο.
DiskSpaceMBLabel=Απαιτούνται τουλάχιστον [mb] MB ελεύθερου χώρου στο δίσκο.
CannotInstallToNetworkDrive=Η εγκατάσταση δεν μπορεί να γίνει σε δίσκο δικτύου.
CannotInstallToUNCPath=Η εγκατάσταση δεν μπορεί να γίνει σε διαδρομή UNC.
InvalidPath=Πρέπει να δώσετε την πλήρη διαδρομή με το γράμμα δίσκου, για παράδειγμα:%n%nC:\APP%n%nή μια διαδρομή UNC της μορφής:%n%n\\server\share
InvalidDrive=Ο τοπικός δίσκος ή ο δίσκος δικτύου που έχετε επιλέξει δεν υπάρχει ή δεν είναι προσβάσιμος. Παρακαλώ, επιλέξτε άλλον.
DiskSpaceWarningTitle=Ανεπαρκής Χώρος στο Δίσκο
DiskSpaceWarning=Η εγκατάσταση χρειάζεται τουλάχιστον %1 KB ελεύθερο χώρο στο δίσκο αλλά ο επιλεγμένος δίσκος διαθέτει μόνον %2 KB.%n%nΘέλετε να συνεχίσετε παρόλα αυτά;
DirNameTooLong=Το όνομα ή η διαδρομή του φακέλου είναι πολύ μεγάλη.
InvalidDirName=Το όνομα του φακέλου δεν είναι έγκυρο.
BadDirName32=Το όνομα του φακέλου δεν μπορεί να περιλαμβάνει κανέναν από τους παρακάτω χαρακτήρες:%n%n%1
DirExistsTitle=Ο Φάκελος Υπάρχει
DirExists=Ο φάκελος:%n%n%1%n%nυπάρχει ήδη. Θέλετε να γίνει η εγκατάσταση σε αυτόν τον φάκελο παρόλα αυτά;
DirDoesntExistTitle=Ο Φάκελος Δεν Υπάρχει
DirDoesntExist=Ο φάκελος:%n%n%1%n%nδεν υπάρχει. Θέλετε να δημιουργηθεί;

; *** "Select Components" wizard page
WizardSelectComponents=Επιλογή Λειτουργιών Μονάδων
SelectComponentsDesc=Ποια στοιχεία θέλετε να εγκατασταθούν;
SelectComponentsLabel2=Επιλέξτε τα στοιχεία που θέλετε να εγκαταστήσετε, αποεπιλέξτε τα στοιχεία που δεν θέλετε να εγκαταστήσετε. Πατήστε Επόμενο όταν είστε έτοιμοι να συνεχίσετε.
FullInstallation=Πλήρης εγκατάσταση
; if possible don't translate 'Compact' as 'Minimal' (I mean 'Minimal' in your language)
CompactInstallation=Τυπική εγκατάσταση
CustomInstallation=Προσαρμοσμένη εγκατάσταση
NoUninstallWarningTitle=Οι Λειτουργικές Μονάδες Υπάρχουν
NoUninstallWarning=Ο Οδηγός Εγκατάστασης εντόπισε ότι τα ακόλουθα στοιχεία είναι ήδη εγκατεστημένα στον υπολογιστή σας:%n%n%1%n%nΑποεπιλέγοντας αυτά τα στοιχεία δεν θα απεγκατασταθούν.%n%nΘέλετε να συνεχίσετε παρόλα αυτά;
ComponentSize1=%1 KB
ComponentSize2=%1 MB
ComponentsDiskSpaceGBLabel=Η τρέχουσα επιλογή απαιτεί τουλάχιστον [gb] GB χώρου στο δίσκο.
ComponentsDiskSpaceMBLabel=Η τρέχουσα επιλογή απαιτεί τουλάχιστον [mb] MB χώρου στο δίσκο.

; *** "Select Additional Tasks" wizard page
WizardSelectTasks=Επιλογή Επιπλέον Ενεργειών
SelectTasksDesc=Ποιες επιπλέον ενέργειες θέλετε να γίνουν;
SelectTasksLabel2=Επιλέξτε τις επιπλέον ενέργειες που θέλετε να γίνουν κατά την εγκατάσταση του [name] και πατήστε Επόμενο.

; *** "Select Start Menu Folder" wizard page
WizardSelectProgramGroup=Επιλογή Φακέλου Μενού Έναρξης
SelectStartMenuFolderDesc=Πού θέλετε να τοποθετηθούν οι συντομεύσεις του προγράμματος;
SelectStartMenuFolderLabel3=Η εγκατάσταση θα δημιουργήσει τις συντομεύσεις του προγράμματος στον ακόλουθο φάκελο του μενού Έναρξη.
SelectStartMenuFolderBrowseLabel=Για να συνεχίσετε, πατήστε Επόμενο. Αν θέλετε διαφορετικό φάκελο, πατήστε Αναζήτηση.
MustEnterGroupName=Πρέπει να εισαγάγετε ένα όνομα φακέλου.
GroupNameTooLong=Το όνομα ή η διαδρομή του φακέλου είναι πολύ μεγάλη.
InvalidGroupName=Το όνομα του φακέλου δεν είναι έγκυρο.
BadGroupName=Το όνομα του φακέλου δεν μπορεί να περιλαμβάνει κανέναν από τους παρακάτω χαρακτήρες:%n%n%1
NoProgramGroupCheck2=&Χωρίς δημιουργία φακέλου στο μενού Έναρξης.

; *** "Ready to Install" wizard page
WizardReady=Έτοιμα για Εγκατάσταση
ReadyLabel1=Ο Οδηγός Εγκατάστασης είναι έτοιμος να ξεκινήσει την εγκατάσταση του [name] στον υπολογιστή σας.
ReadyLabel2a=Πατήστε Εγκατάσταση για να συνεχίσετε με την εγκατάσταση ή πατήστε Πίσω, εάν θέλετε να ελέγξετε ή να αλλάξετε τυχόν ρυθμίσεις.
ReadyLabel2b=Πατήστε Εγκατάσταση για να συνεχίσετε την εγκατάσταση.
ReadyMemoUserInfo=Πληροφορίες Χρήστη:
ReadyMemoDir=Φάκελος προορισμού:
ReadyMemoType=Είδος εγκατάστασης:
ReadyMemoComponents=Επιλεγμένες λειτουργικές μονάδες:
ReadyMemoGroup=Φάκελος στο μενού Έναρξη:
ReadyMemoTasks=Επιπλέον ενέργειες:

; *** TDownloadWizardPage wizard page and DownloadTemporaryFile
DownloadingLabel=Λήψη πρόσθετων αρχείων...
ButtonStopDownload=&Διακοπή λήψης
StopDownload=Είστε βέβαιοι ότι θέλετε να διακόψετε τη λήψη;
ErrorDownloadAborted=Η λήψη ακυρώθηκε
ErrorDownloadFailed=Η λήψη απέτυχε: %1 %2
ErrorDownloadSizeFailed=Η λήψη του μεγέθους απέτυχε: %1 %2
ErrorFileHash1=Αποτυχία υπολογισμού hash: %1
ErrorFileHash2=Μη έγκυρο hash: αναμενόμενο %1, βρέθηκε %2
ErrorProgress=Μη έγκυρη πρόοδος: %1 από %2
ErrorFileSize=Μη έγκυρο μέγεθος αρχείου: αναμενόμενο %1, βρέθηκε %2

; *** "Preparing to Install" wizard page
WizardPreparing=Προετοιμασία Εγκατάστασης
PreparingDesc=Ο Οδηγός Εγκατάστασης προετοιμάζεται για την εγκατάσταση του [name] στον υπολογιστή σας.
PreviousInstallNotCompleted=Η εγκατάσταση/αφαίρεση ενός προηγούμενου προγράμματος δεν ολοκληρώθηκε. Θα χρειαστεί να κάνετε επανεκκίνηση του υπολογιστή σας για να ολοκληρωθεί.%n%nΜετά την επανεκκίνηση του υπολογιστή σας, εκτελέστε ξανά τον Οδηγό Εγκατάστασης για να ολοκληρώσετε την εγκατάσταση/αφαίρεση του [name].
CannotContinue=Η εγκατάσταση δεν μπορεί να συνεχιστεί. Παρακαλώ πατήστε Άκυρο για τερματισμό.
ApplicationsFound=Οι ακόλουθες εφαρμογές χρησιμοποιούν αρχεία που πρέπει να ενημερωθούν από τον Οδηγό Εγκατάστασης. Συνιστάται να επιτρέψετε στον Οδηγό Εγκατάστασης να κλείσει αυτόματα αυτές τις εφαρμογές.
ApplicationsFound2=Οι ακόλουθες εφαρμογές χρησιμοποιούν αρχεία που πρέπει να ενημερωθούν από τον Οδηγό Εγκατάστασης. Συνιστάται να επιτρέψετε στον Οδηγό Εγκατάστασης να κλείσει αυτόματα αυτές τις εφαρμογές. Μετά την ολοκλήρωση της εγκατάστασης, ο Οδηγός Εγκατάστασης θα επιχειρήσει να κάνει επανεκκίνηση των εφαρμογών.
CloseApplications=&Αυτόματο κλείσιμο των εφαρμογών
DontCloseApplications=&Χωρίς κλείσιμο των εφαρμογών
ErrorCloseApplications=Ο Οδηγός Εγκατάστασης δεν μπόρεσε να κλείσει αυτόματα όλες τις εφαρμογές. Συνιστάται να κλείσετε όλες τις εφαρμογές που χρησιμοποιούν αρχεία που πρέπει να ενημερωθούν από τον Οδηγό Εγκατάστασης προτού συνεχίσετε.
PrepareToInstallNeedsRestart=Ο Οδηγός Εγκατάστασης πρέπει να κάνει επανεκκίνηση του υπολογιστή σας. Μετά την επανεκκίνηση του υπολογιστή σας, εκτελέστε ξανά τον Οδηγό Εγκατάστασης για να ολοκληρώσετε την εγκατάσταση του [name].%n%nΘα θέλατε να κάνετε επανεκκίνηση τώρα;

; *** "Installing" wizard page
WizardInstalling=Εγκατάσταση
InstallingLabel=Παρακαλώ περιμένετε καθώς γίνεται η εγκατάσταση του [name] στον υπολογιστή σας.

; *** "Setup Completed" wizard page
FinishedHeadingLabel=Ολοκλήρωση του Οδηγού Εγκατάστασης του [name]
FinishedLabelNoIcons=Ο Οδηγός Εγκατάστασης ολοκλήρωσε την εγκατάσταση του [name] στον υπολογιστή σας.
FinishedLabel=Ο Οδηγός Εγκατάστασης ολοκλήρωσε την εγκατάσταση του [name] στον υπολογιστή σας. Η εφαρμογή μπορεί να ξεκινήσει επιλέγοντας κάποια από τις εγκατεστημένες συντομεύσεις.
ClickFinish=Πατήστε Τέλος για να τερματίσετε τον Οδηγό Εγκατάστασης.
FinishedRestartLabel=Για να ολοκληρώσετε την εγκατάσταση του [name], ο Οδηγός Εγκατάστασης πρέπει να κάνει επανεκκίνηση του υπολογιστή σας. Θα θέλατε να κάνετε επανεκκίνηση τώρα;
FinishedRestartMessage=Για να ολοκληρώσετε την εγκατάσταση του [name], ο Οδηγός Εγκατάστασης πρέπει να κάνει επανεκκίνηση του υπολογιστή σας.%n%nΘα θέλατε να κάνετε επανεκκίνηση τώρα;
ShowReadmeCheck=Ναι, θα ήθελα να δω το αρχείο README
YesRadio=&Ναι, να γίνει επανεκκίνηση τώρα
NoRadio=&Οχι, θα κάνω επανεκκίνηση αργότερα
; used for example as 'Run MyProg.exe'
RunEntryExec=Εκτέλεση του %1
; used for example as 'View Readme.txt'
RunEntryShellExec=Προβολή του %1

; *** "Setup Needs the Next Disk" stuff
ChangeDiskTitle=Ο Οδηγός Εγκατάστασης χρειάζεται τον επόμενο δίσκο
SelectDiskLabel2=Παρακαλώ, εισάγετε τον δίσκο %1 και πατήστε ΟΚ.%n%nΕάν τα αρχεία αυτού του δίσκου βρίσκονται σε φάκελο διαφορετικό από αυτόν που εμφανίζεται παρακάτω, πληκτρολογήστε τη σωστή διαδρομή ή πατήστε Αναζήτηση.
PathLabel=&Διαδρομή:
FileNotInDir2=Το αρχείο "%1" δε βρέθηκε στο "%2". Παρακαλώ εισάγετε το σωστό δίσκο ή επιλέξτε κάποιον άλλο φάκελο.
SelectDirectoryLabel=Παρακαλώ καθορίσετε την τοποθεσία του επόμενου δίσκου.

; *** Installation phase messages
SetupAborted=Η εγκατάσταση δεν ολοκληρώθηκε.%n%nΠαρακαλώ, διορθώστε το πρόβλημα και εκτελέστε ξανά τον Οδηγό Εγκατάστασης.
AbortRetryIgnoreSelectAction=Επιλέξτε ενέργεια
AbortRetryIgnoreRetry=&Δοκιμή
AbortRetryIgnoreIgnore=&Αγνόηση και συνέχεια
AbortRetryIgnoreCancel=Ακυρώση εγκατάστασης

; *** Installation status messages
StatusClosingApplications=Κλείσιμο εφαρμογών...
StatusCreateDirs=Δημιουργία φακέλων...
StatusExtractFiles=Αποσυμπίεση αρχείων...
StatusCreateIcons=Δημιουργία συντομεύσεων...
StatusCreateIniEntries=Δημιουργία καταχωρήσεων INI...
StatusCreateRegistryEntries=Δημιουργία καταχωρήσεων στο μητρώο...
StatusRegisterFiles=Καταχώρηση αρχείων...
StatusSavingUninstall=Αποθήκευση πληροφοριών απεγκατάστασης...
StatusRunProgram=Ολοκλήρωση εγκατάστασης...
StatusRestartingApplications=Επανεκκίνηση εφαρμογών...
StatusRollback=Επαναφορά αλλαγών...

; *** Misc. errors
ErrorInternal2=Εσωτερικό σφάλμα: %1
ErrorFunctionFailedNoCode=%1 απέτυχε
ErrorFunctionFailed=%1 απέτυχε, κωδικός %2
ErrorFunctionFailedWithMessage=%1 απέτυχε, κωδικός %2.%n%3
ErrorExecutingProgram=Δεν είναι δυνατή η εκτέλεση του αρχείου:%n%1

; *** Registry errors
ErrorRegOpenKey=Σφάλμα ανάγνωσης κλειδιού μητρώου:%n%1\%2
ErrorRegCreateKey=Σφάλμα δημιουργίας κλειδιού μητρώου:%n%1\%2
ErrorRegWriteKey=Σφάλμα καταχώρησης κλειδιού μητρώου:%n%1\%2

; *** INI errors
ErrorIniEntry=Σφάλμα στη δημιουργία καταχώρησης INI στο αρχείο "%1".

; *** File copying errors
FileAbortRetryIgnoreSkipNotRecommended=&Παράλειψη αυτού του αρχείου (δεν συνιστάται)
FileAbortRetryIgnoreIgnoreNotRecommended=Παράλειψη σφάλματος και &συνέχεια (δεν συνιστάται)
SourceIsCorrupted=Το αρχείο προέλευσης είναι κατεστραμμένο
SourceDoesntExist=Το αρχείο προέλευσης "%1" δεν υπάρχει
ExistingFileReadOnly2=Το υπάρχον αρχείο δεν μπόρεσε να αντικατασταθεί επειδή είναι μόνο για ανάγνωση.
ExistingFileReadOnlyRetry=&Καταργήστε το χαρακτηριστικό μόνο για ανάγνωση και δοκιμάστε ξανά
ExistingFileReadOnlyKeepExisting=&Διατηρήστε το υπάρχον αρχείο
ErrorReadingExistingDest=Παρουσιάστηκε σφάλμα κατά την προσπάθεια ανάγνωσης του υπάρχοντος αρχείου:
FileExistsSelectAction=Επιλέξτε ενέργεια
FileExists2=Το αρχείο υπάρχει ήδη.
FileExistsOverwriteExisting=&Αντικατάσταση υπάρχοντος αρχείου
FileExistsKeepExisting=&Διατήρηση υπάρχοντος αρχείου
FileExistsOverwriteOrKeepAll=&Να γίνει το ίδιο για τις επόμενες διενέξεις
ExistingFileNewerSelectAction=Επιλέξτε ενέργεια
ExistingFileNewer2=Το υπάρχον αρχείο είναι νεότερο από αυτό που προσπαθεί να εγκαταστήσει ο Οδηγός Εγκατάστασης.
ExistingFileNewerOverwriteExisting=&Αντικατάσταση υπάρχοντος αρχείου
ExistingFileNewerKeepExisting=&Διατήρηση υπάρχοντος αρχείου (συνιστάται)
ExistingFileNewerOverwriteOrKeepAll=&Να γίνει το ίδιο για τις επόμενες διενέξεις
ErrorChangingAttr=Παρουσιάστηκε σφάλμα κατά την προσπάθεια αλλαγής των χαρακτηριστικών του υπάρχοντος αρχείου:
ErrorCreatingTemp=Παρουσιάστηκε σφάλμα κατά την προσπάθεια δημιουργίας ενός αρχείου στον φακέλο προορισμού:
ErrorReadingSource=Παρουσιάστηκε σφάλμα κατά την προσπάθεια ανάγνωσης του αρχείου προέλευσης:
ErrorCopying=Παρουσιάστηκε σφάλμα κατά την προσπάθεια αντιγραφής ενός αρχείου:
ErrorReplacingExistingFile=Παρουσιάστηκε σφάλμα κατά την προσπάθεια αντικατάστασης του υπάρχοντος αρχείου:
ErrorRestartReplace=Η ΕπανεκκίνησηΑντικατάσταση απέτυχε:
ErrorRenamingTemp=Παρουσιάστηκε σφάλμα κατά την προσπάθεια μετονομασίας ενός αρχείου στον φακέλο προορισμού:
ErrorRegisterServer=Δεν είναι δυνατή η καταχώριση του DLL/OCX: %1
ErrorRegSvr32Failed=Το RegSvr32 απέτυχε με κωδικό εξόδου %1
ErrorRegisterTypeLib=Δεν είναι δυνατή η καταχώριση της βιβλιοθήκης τύπων: %1

; *** Uninstall display name markings
; used for example as 'My Program (32-bit)'
UninstallDisplayNameMark=%1 (%2)
; used for example as 'My Program (32-bit, All users)'
UninstallDisplayNameMarks=%1 (%2, %3)
UninstallDisplayNameMark32Bit=32-bit
UninstallDisplayNameMark64Bit=64-bit
UninstallDisplayNameMarkAllUsers=Ολοι οι χρήστες
UninstallDisplayNameMarkCurrentUser=Τρέχων χρήστης

; *** Post-installation errors
ErrorOpeningReadme=Παρουσιάστηκε σφάλμα κατά την προσπάθεια ανοίγματος του αρχείου README.
ErrorRestartingComputer=Ο Οδηγός Εγκατάστασης δεν μπόρεσε να κάνει επανεκκίνηση του υπολογιστή. Παρακαλώ επανεκκινήσετε τον υπολογιστή μόνοι σας.

; *** Uninstaller messages
UninstallNotFound=Το αρχείο "%1" δεν υπάρχει. Δεν είναι δυνατή η απεγκατάσταση.
UninstallOpenError=Το αρχείο "%1" δεν ήταν δυνατό να ανοίξει. Δεν είναι δυνατή η απεγκατάσταση
UninstallUnsupportedVer=Το αρχείο καταγραφής απεγκατάστασης "%1" είναι σε μορφή που δεν αναγνωρίζεται από αυτήν την έκδοση του Οδηγού Απεγκατάστασης. Δεν ήταν δυνατή η απεγκατάσταση
UninstallUnknownEntry=Μια άγνωστη καταχώρηση (%1) εντοπίστηκε στο αρχείο καταγραφής απεγκατάστασης
ConfirmUninstall=Είστε βέβαιοι ότι θέλετε να καταργήσετε εντελώς το %1 και όλα τα στοιχεία του;
UninstallOnlyOnWin64=Αυτή η εγκατάσταση μπορεί να απεγκατασταθεί μόνο σε Windows 64-bit.
OnlyAdminCanUninstall=Αυτή η εγκατάσταση μπορεί να απεγκατασταθεί μόνο από χρήστη με δικαιώματα διαχειριστή.
UninstallStatusLabel=Παρακαλώ περιμένετε μέχρι να καταργηθεί το %1 από τον υπολογιστή σας.
UninstalledAll=Το %1 αφαιρέθηκε με επιτυχία από τον υπολογιστή σας.
UninstalledMost=Το %1 αφαιρέθηκε με επιτυχία.%n%nΟρισμένα στοιχεία δεν ήταν δυνατό να καταργηθούν. Αυτά μπορούν να αφαιρεθούν από εσάς.
UninstalledAndNeedsRestart=Για να ολοκληρώσετε την απεγκατάσταση του %1, ο υπολογιστής σας πρέπει να επανεκκινηθεί.%n%nΘα θέλατε να κάνετε επανεκκίνηση τώρα;
UninstallDataCorrupted=Το "%1" αρχείο είναι κατεστραμμένο. Δεν ήταν δυνατή η απεγκατάσταση

; *** Uninstallation phase messages
ConfirmDeleteSharedFileTitle=Κατάργηση Κοινόχρηστου Αρχείου;
ConfirmDeleteSharedFile2=Το σύστημα υποδεικνύει ότι το ακόλουθο κοινόχρηστο αρχείο δεν χρησιμοποιείται πλέον από κανένα πρόγραμμα. Θέλετε να καταργηθεί αυτό το κοινόχρηστο αρχείο;%n%nΕάν κάποιο πρόγραμμα εξακολουθεί να το χρησιμοποιεί, ενδέχεται να μην λειτουργήσει σωστά. Εάν δεν είστε βέβαιοι, επιλέξτε Όχι. Αφήνοντάς το στο σύστημά σας δεν θα προκληθεί καμία ζημιά.
SharedFileNameLabel=Όνομα Αρχείου:
SharedFileLocationLabel=Τοποθεσία:
WizardUninstalling=Πρόοδος Απεγκατάστασης
StatusUninstalling=Απεγκατάσταση %1...

; *** Shutdown block reasons
ShutdownBlockReasonInstallingApp=Εγκατάσταση του %1.
ShutdownBlockReasonUninstallingApp=Απεγκατάσταση του %1.

; The custom messages below aren't used by Setup itself, but if you make
; use of them in your scripts, you'll want to translate them.

[CustomMessages]

NameAndVersion=%1 έκδοση %2
AdditionalIcons=Επιπλέον συντομεύσεις:
CreateDesktopIcon=Δημιουργία συντόμευσης στην &επιφάνεια εργασίας
CreateQuickLaunchIcon=Δημιουργία συντόμευσης στη &Γρήγορη Εκκίνηση
ProgramOnTheWeb=Το %1 στο Internet
UninstallProgram=Απεγκατάσταση του %1
LaunchProgram=Εκκίνηση του %1
AssocFileExtension=&Συσχέτιση του %1 με την επέκταση αρχείου %2 
AssocingFileExtension=Γίνεται συσχέτιση του %1 με την επέκταση αρχείου "%2"...
AutoStartProgramGroupDescription=Εκκίνηση:
AutoStartProgram=Αυτόματη εκκίνηση του %1
AddonHostProgramNotFound=Το %1 δε βρέθηκε στο φάκελο που επιλέξατε.%n%nΘέλετε να συνεχίσετε παρόλα αυτά;

