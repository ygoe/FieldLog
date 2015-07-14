<?php
// Encoding: UTF-8 w/o BOM - °°°°°

// FieldLog – .NET logging solution
// © Yves Goergen, Made in Germany
// Licence: GNU GPL v3
// Website: http://unclassified.software/source/fieldlog

// FieldLog submit web API
//
// Accepts uploaded log archives and sends them to a registered e-mail address or stores them
// on the server for later download. This script can handle multiple registered recipients and
// host log redirection services to multiple independent users through the use of API tokens.
// A token can be generated interactively by the script. Tokens and registration data must be
// entered in this script file; there are no additional configuration files.
//
// NOTE: Set the configuration and registrations in the GetConfiguration() and
// GetRegistrationData() methods. Other parts of this script should not be altered.
//
// NOTE: There is no maximum accepted size for uploaded files. Make sure you have sufficient
// disk space and quota when running this script. Also make sure the execution environment
// accepts large uploaded files. This may affect web server and PHP configuration settings.
//
// PRIVACY: You should configure your web server to prevent directory listings in the data
// directory and prevent file downloads in the log directory.

Main();

// Local configuration data.
//
function GetConfiguration()
{
	return array(
		// Publicly accessible URL to the log archive directory, including the trailing slash.
		'baseAddress' => 'https://example.com/api/fieldlog/data/',
		// Local path to the log archive directory. Will be created.
		'dataPath' => 'data',
		// Local path to the log files. May contain date pattern placeholders in braces.
		// The directory will be created. If unset, no log is written.
		'logFile' => 'log/logsubmit.{Y-m}.log',
		// Number of days to keep uploaded files before deleting them (default: 30).
		'keepDays' => 30,
		);
}

// Defines API tokens and associated redirection e-mail addresses.
//
function GetRegistrationData()
{
	return array(
		// Define an entry for each registered API token.
		'12345678901234567890' => array(
			// The recipient address for e-mail messages.
			'eMail' => 'mail@example.com',
			// The subject used in e-mail messages.
			'subject' => 'Log archive submitted by FieldLog',
			// The maximum file size to send directly, in bytes (default: 1 MiB).
			// Bigger files are stored on the server and a download link is sent instead.
			'maxSendSize' => 1048576,
			),
		);
}

// Main program.
//
function Main()
{
	// Handle special requests
	if ($_GET['purge'])
	{
		WriteLog('Purging on demand');
		PurgeFiles();
		exit();   // Be silent, this could be a cron job
	}
	if (!$_POST)
	{
		ExitShowForm();
	}
	if ($_POST['generateToken'])
	{
		ExitGenerateToken();
	}

	// Process uploaded log file data
	// Check token
	$token = $_POST['token'];
	if (!$token)
		ExitMessage(400, 'Token not specified.');
	$data = GetRegistrationData();
	$tokenData = $data[$token];
	if (!$tokenData)
		ExitMessage(404, 'Invalid token.');

	// Check file
	if (!isset($_FILES['logFile']))
		ExitMessage(400, 'Missing log file in uploaded form data.');
	$fileName = $_FILES['logFile']['tmp_name'];
	if (!$fileName)
		ExitMessage(500, 'Uploaded file not found.');
	$size = filesize($fileName);
	if ($size == 0)
		ExitMessage(400, 'Uploaded file is empty.');
	$sha1 = sha1_file($fileName);
	$newFileName = $_FILES['logFile']['name'];
	if (preg_match('_["*/:<>?\\|]_', $newFileName))
		ExitMessage(400, 'Invalid file name.');
	if (strlen($newFileName) > 60)
		ExitMessage(400, 'Invalid file name.');
	WriteLog('Uploaded file "' . $newFileName . '" (' . $size . ' bytes)');

	// Send or store file
	$maxSendSize = $tokenData['maxSendSize'];
	if (!isset($maxSendSize)) $maxSendSize = 1024 * 1024;   // Default to 1 MiB
	if ($size <= $maxSendSize)
	{
		SendMail($tokenData['eMail'], $tokenData['subject'], $fileName, $newFileName);
	}
	else
	{
		// Add random to prevent guessing file names to download
		$random = GenerateRandom(5);
		$newFileName = $token . '_' . $random . '_' . $newFileName;
		SaveFileAndSendNotification($tokenData['eMail'], $tokenData['subject'], $fileName, $newFileName);
	}
	
	PurgeFiles();
	ExitMessage(200, 'OK ' . $token . ' ' . $size . ' ' . $sha1);
}

// Shows the HTML form to manually submit a log file.
//
function ExitShowForm()
{
	$token = htmlspecialchars($_REQUEST['token']);
	header('Content-Type: text/html; charset=UTF-8');
	?>
	<!doctype html>
	<html>
		<head>
			<title>FieldLog submit web API</title>
		</head>
		<body>
			<h1>FieldLog submit web API</h1>
			<form method="post" enctype="multipart/form-data">
				<p>
					API token: <input type="text" name="token" size="50" value="<?php echo $token?>">
				</p>
				<p>
					Log file archive: <input type="file" name="logFile">
				</p>
				<p>
					<input type="submit">
				</p>
			</form>
			<hr>
			<form method="post">
				<p>
					<input type="submit" name="generateToken" value="Generate unique token">
				</p>
			</form>
			<footer>
				<a href="http://unclassified.software/source/fieldlog">FieldLog website</a>
			</footer>
		</body>
	</html>
	<?php
	exit();
}

// Generates a random string.
//
function GenerateRandom($length)
{
	$rnd = '';
	for ($pos = 0; $pos < $length; $pos++)
	{
		$i = mt_rand(0, 61);
		if ($i < 10) $c = chr($i + 48);
		else if ($i < 10 + 26) $c = chr($i - 10 + 65);
		else $c = chr($i - 10 - 26 + 97);
		$rnd .= $c;
	}
	return $rnd;
}

// Generates a new unique token and prints it.
//
function ExitGenerateToken()
{
	$data = GetRegistrationData();
	do
	{
		$token = GenerateRandom(20);
	}
	while ($data[$token]);
	
	header('Content-Type: text/plain; charset=UTF-8');
	echo $token;
	exit();
}

// Sends an e-mail with the attached file to the specified e-mail address.
//
function SendMail($address, $subject, $fileName, $newFileName)
{
	$mimeBoundary = GenerateRandom(30);
	$headers = "Content-Type: multipart/mixed; boundary=\"" . $mimeBoundary . "\"";
	$attachment = chunk_split(base64_encode(file_get_contents($fileName)));
	$body = "--{$mimeBoundary}\r\n" .
		"Content-Type: text/plain; charset=\"UTF-8\"\r\n\r\n" .
		"This is a log archive sent through the FieldLog submit web API.\r\n\r\n" .
		"--{$mimeBoundary}\r\n" .
		"Content-Type: application/x-lzma; name=\"{$newFileName}\"\r\n" .
		"Content-Transfer-Encoding: base64\r\n" .
		"Content-Disposition: attachment\r\n\r\n" .
		$attachment .
		"--{$mimeBoundary}--\r\n";

	WriteLog('Sending uploaded file to ' . $address);
	if (!mail($address, $subject, $body, $headers))
		ExitMessage(500, 'E-mail could not be sent.');
}

// Saves a file locally and sends an e-mail notification with a download link.
//
function SaveFileAndSendNotification($address, $subject, $fileName, $newFileName)
{
	$config = GetConfiguration();
	if (!is_dir($config['dataPath'])) mkdir($config['dataPath'], 0777, true);

	// Make file name unique by adding a counter in front of the first "."
	// (Yes, it's highly unlikely that two file names are the same, given the additional
	// random part, but this is safe.)
	$count = 0;
	$uniqueFileName = $newFileName;
	while (file_exists($config['dataPath'] . '/' . $uniqueFileName))
	{
		$count++;
		$uniqueFileName = preg_replace('_\._', '[' . $count . '].', $newFileName, 1);
	}

	// Save uploaded file to data directory
	$size = filesize($fileName);
	if (!move_uploaded_file($fileName, $config['dataPath'] . '/' . $uniqueFileName))
		ExitMessage(500, 'Uploaded file could not be stored.');

	$config = GetConfiguration();
	$keepDays = $config['keepDays'];
	if ($keepDays < 1) $keepDays = 30;
	
	// Send notification e-mail
	$headers = "Content-Type: text/plain; charset=\"UTF-8\"";
	$body = "A log archive was sent through the FieldLog submit web API.\r\n" .
		"You can download it within {$keepDays} days from the following URL:\r\n" .
		$config['baseAddress'] . $uniqueFileName . "\r\n" .
		"The file size is " . round($size / 1024) . " KiB.\r\n";

	WriteLog('Sending notification to ' . $address);
	if (!mail($address, $subject, $body, $headers))
		ExitMessage(500, 'Notification e-mail could not be sent.');
}

// Deletes locally stored log files older than 30 days.
//
function PurgeFiles()
{
	$config = GetConfiguration();
	if (!is_dir($config['dataPath'])) return;   // Nothing to do
	
	$keepDays = $config['keepDays'];
	if ($keepDays < 1) $keepDays = 30;
	
	$minTime = time() - $keepDays * 86400;
	$deletedCount = 0;
	foreach (glob($config['dataPath'] . '/*') as $filename)
	{
		if (filemtime($filename) < $minTime)
		{
			unlink($filename);
			$deletedCount++;
		}
	}
	if ($deletedCount > 0)
	{
		WriteLog('Deleted ' . $deletedCount . ' files older than ' . $keepDays . ' days');
	}
}

// Exits with a text response and HTTP status code.
//
function ExitMessage($code, $text)
{
	WriteLog('Output: ' . $code . ', ' . $text);

	header($_SERVER['SERVER_PROTOCOL'] . ' ' . $code);
	header('Content-Type: text/plain; charset=UTF-8');
	echo $text;
	exit();
}

// Writes an event to the log file.
//
function WriteLog($message)
{
	$logLine = date('Y-m-d H:i:s') . ' ' . $_SERVER['REMOTE_ADDR'] . ' ' . $message . "\n";
	
	$config = GetConfiguration();
	$logFile = $config['logFile'];
	if (!strlen($logFile)) return;
	
	// Resolve date pattern
	$logFile = preg_replace_callback(
		'_\{(.+)\}_',
		function ($m)
		{
			return date($m[1]);
		},
		$logFile);
	
	// Create directory
	$logDir = dirname($logFile);
	if (strlen($logDir) && !is_dir($logDir))
	{
		mkdir($logDir, 0777, true);
	}

	// Append to log file
	file_put_contents($logFile, $logLine, FILE_APPEND);
}
