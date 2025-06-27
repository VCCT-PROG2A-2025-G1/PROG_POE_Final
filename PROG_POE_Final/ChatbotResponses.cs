using PROG_POE_Final;
using PROG_POE_Final.Models;
using PROG_POE_Final.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PROG_POE
{
    class ChatbotResponses
    {
        Dictionary<string, List<string>> sentimentResponses = new Dictionary<string, List<string>>();   // Dictionary to map chatbot responses to lists of possible user inputs
        Dictionary<string, List<string>> keywordResponses = new Dictionary<string, List<string>>();     // Dictionary to map chatbot responses to lists of possible keywords

        // Dictionary to map follow-up responses with possible user follow-up questions.
        // Code structure taken from OpenAI ChatGPT to resolve previous dictionary logic with a string and List key-value pair.
        Dictionary<string, Dictionary<string, string>> followUpsAnswers = new Dictionary<string, Dictionary<string, string>>();

        Dictionary<string, string> keywordToMainTopic = new Dictionary<string, string>();   // Dictionary to map current topic synonyms to followUpsAnswer Keys
        Dictionary<string, string> memory = new Dictionary<string, string>();   // Dictionary to store memory of name and favourite topic
        List<string> followUpPrompts;
        Random rand = new Random();
        private string currentMode = "normal";
        private string pendingStatus = null;  // Tracks "complete" or "delete" waiting for a number

        string currentTopic = null;
        string currentSubTopic = null;
        bool awaitFollowUp = false;

        public TaskManager taskManager = new TaskManager();
        public QuizManager quizManager = new QuizManager();
        public ActivityLog activityLog = new ActivityLog();

        private Dictionary<string, List<string>> intentKeywords = new Dictionary<string, List<string>>()
        {
            { "AddTask", new List<string> { "add task", "create task", "new task", "set task", "remind me", "add reminder", "set reminder" } },
            { "ViewTasks", new List<string> { "show tasks", "view tasks", "list tasks", "my tasks" } },
            { "CompleteTask", new List<string> { "complete task", "done task", "finish task", "mark task" } },
            { "DeleteTask", new List<string> { "delete task", "remove task", "discard task" } },
            { "StartQuiz", new List<string> { "start quiz", "take quiz", "begin quiz", "cyber quiz", "quiz game" } },
            { "AnswerQuiz", new List<string> { "answer", "choose", "pick", "select option" } },
            { "ShowLog", new List<string> { "show activity log", "what have you done", "show log", "activity log" } }
        };


        //---------------------------------------------------------------------------------------------------------------------------
        //Default Constructor
        public ChatbotResponses(string userName)
        {
            KeywordResponses(); //Load keyword responses into the dictionary
            FollowUpAnswers(); //Load followup answers into the dictionary
            KeywordToMainTopic();
            InitialiseSentimentResponses();
            if (!string.IsNullOrEmpty(userName)) // validate if user's name is not empty
            {
                memory["name"] = userName.ToLower();  // store name in lowercase for easy comparison
            }
        }//-------------------------------------------------------------
        //Clears logs and previous chat and restarts the chat
        public void RestartSession()
        {
            activityLog.ClearLogs();
            quizManager.RestartQuiz();
        }

        //View tasks via button click
        public string ViewTasks()
        {
            string tasks = taskManager.DisplayTasks();
            activityLog.AddLog("Viewed tasks.");
            return tasks;
        }
        //---------------------------------------------------------------------------------------------------------------------------
        //Returns a chatbot response based on the user's input
        public string GetResponse(string input)
        {
            // validates if the user input is null
            if (string.IsNullOrEmpty(input))
            {
                return "You seemed to have not entered anything.\nFeel free to ask me a question";  // Return message if input is null or empty
            }

            input = input.Trim();      // Remove leading whitespace
            input = input.ToLower();   // Convert input to lowercase for comparison
            //Sourced from OpenAI ChatGPT
            input = input.TrimEnd('.', '!', '?'); // Remove ending punctuation
            //initialize stringbuilder for bot response

            StringBuilder bot = new StringBuilder();


            if (input.Contains("add task") || input.Contains("set a reminder") || input.Contains("remind me") || input.Contains("create a task") || input.Contains("add a task"))
            {
                var (title, reminder) = TaskNLP.ParseTask(input);
                taskManager.AddTask(title, "", reminder);
                activityLog.AddLog($"Task added: {title}" + (reminder.HasValue ? $" with reminder for {reminder}" : ""));
                currentMode = "task";
                return $"Task added: \"{title}\"" + (reminder.HasValue ? $" with reminder for {reminder.Value:g}" : "") + ".";
            }

            if (input.Contains("show tasks") || input.Contains("view tasks") || input.Contains("list tasks") || input.Contains("my tasks"))
            {
                currentMode = "task";
                activityLog.AddLog("Viewed tasks.");
                return taskManager.DisplayTasks();
            }

            if (input.Contains("complete task") || input.Contains("completed my task") || input.Contains("finished a task") || input.Contains("finished my task") || input.Contains("task is completed"))
            {
                currentMode = "task";
                pendingStatus = "complete";
                return "Please specify the task number to complete.";
            }

            if (input.Contains("delete task"))
            {
                currentMode = "task";
                pendingStatus = "delete";
                return "Please specify the task number to delete.";
            }
            //------------------------------------------------
            //Quiz commands to trigger starting the quiz
            if (input.Contains("start quiz") || input.Contains("take quiz") || input.Contains("cyber quiz") || input.Contains("take a quiz"))
            {
                quizManager.RestartQuiz();
                var q = quizManager.GetCurrentQuestion();
                activityLog.AddLog("Started quiz.");
                currentMode = "quiz";
                return FormatQuestion(q);
            }

            // -----------------------------------------------------------------------------------------------------------------------------
            // Process numbersentered by user based on currentMode
            if (int.TryParse(input, out int numberInput))
            {
                if (currentMode == "task")
                {
                    // Determine if the number entered by the user if for completion or deletion
                    // Ask the user first be completing or deleting

                    if (pendingStatus=="complete")
                    {
                        taskManager.MarkTaskCompleted(numberInput - 1);
                        activityLog.AddLog($"Marked task {numberInput} as completed.");
                        return $"Task {numberInput} marked as completed.";
                    }
                    else if (pendingStatus=="delete")
                    {
                        taskManager.DeleteTask(numberInput - 1);
                        activityLog.AddLog($"Deleted task {numberInput}.");
                        return $"Task {numberInput} deleted.";
                    }
                    else
                    {
                        return "Are you trying to complete or delete the task? Please specify.";
                    }
                }
                else if (currentMode == "quiz" && quizManager.GetCurrentQuestion() != null)
                {
                    string feedback = quizManager.SubmitAnswer(numberInput - 1);
                    activityLog.AddLog($"Answered quiz question.");

                    var nextQ = quizManager.GetCurrentQuestion();
                    if (nextQ != null)
                        return feedback + "\n\n" + FormatQuestion(nextQ);
                    else
                    {
                        currentMode = "normal"; // Reset mode when quiz is over
                        return feedback + "\nQuiz completed!";
                    }
                }
                else
                {
                    return "Please clarify what you'd like to do with that number.";
                }
            }

            //---------------------------------------------------------------------
            // Display activity log if there are existing logs
            if (input.Contains("show activity log") || input.Contains("what have you done") || input.Contains("show log"))
            {
                return activityLog.GetLogSummary();
            }


            //---------------------------------------------------------------------           
            //checks if user inputs their favourite topic and stores it into the memory dictionary
            if (input.StartsWith("my favourite is ") || input.StartsWith("i like "))
            {
                string topic = input.StartsWith("my favourite is ") ?
                                input.Replace("my favourite is ", "").Trim() :
                                input.Replace("i like ", "").Trim();

                memory["favouriteTopic"] = topic;
                return $"Got it! I'll remember that your favourite topic is {Capitalise(topic)}.";
            }

            //---------------------------------------------------------------------
            //checks if user asks for their favourite topic and retrieves it from the memory dictionary
            if (input.Contains("my favourite topic") && memory.ContainsKey("favouriteTopic"))
            {
                string favTopic = memory["favouriteTopic"];
                return $"You told me your favourite topic is {Capitalise(favTopic)}. Would you like to hear more about it, {Capitalise(memory["name"])}?";
            }

            //---------------------------------------------------------------------
            //checks if the user asks for their name and retrieves it from the memory dictionary
            if (input.Contains("who am i") || input.Contains("what is my name") && memory.ContainsKey("name"))
            {
                return $"You're {Capitalise(memory["name"])}, of course!";
            }

            //---------------------------------------------------------------------
            //checks if the user's sentiment matches negative, positive, or neutral and returns an appropriate response

            foreach (var entry in sentimentResponses)
            {
                if (input.Contains(entry.Key))
                {
                    bot.Append(entry.Value[rand.Next(entry.Value.Count)]);
                    bot.Append("\n\n");
                    break;
                }
            }

            //---------------------------------------------------------------------
            //checks if user wants says yes to wanting to know more about the topic of conversation and further prompts them
            if ((input.Contains("yes") || input.Contains("sure") || input.Contains("okay") || input.Contains("yeah")) && currentTopic != null)
            {
                if (followUpsAnswers.ContainsKey(currentTopic))
                {
                    var followupOptions = followUpsAnswers[currentTopic].Values.ToList();
                    bot.Append(followupOptions[rand.Next(followupOptions.Count)] + "\n");
                }
                else if (keywordResponses.ContainsKey(currentTopic))
                {
                    var moreInfo = keywordResponses[currentTopic];
                    bot.Append(moreInfo[rand.Next(moreInfo.Count)] + "\n");
                }
                else
                {
                    bot.Append("Sure! What else would you like to know?\n");
                }

                bot.Append($"Anything else I can help you with, {Capitalise(memory["name"])}?");
                awaitFollowUp = true;
                return bot.ToString();
            }


            //---------------------------------------------------------------------
            //checks if the followUpsAnswer dictionary contains the current topic
            if (awaitFollowUp && currentTopic != null && followUpsAnswers.ContainsKey(currentTopic))
            {
                foreach (var rPair in followUpsAnswers[currentTopic])
                {
                    if (input.Contains(rPair.Key))
                    {
                        bot.Append(rPair.Value + "\n");
                        currentSubTopic = rPair.Key;
                        return bot.ToString();
                    }
                }
            }

            //---------------------------------------------------------------------
            //checks if user is done chatting with the chatbot
            string[] exitKeywords = { "no", "no thanks", "nah", "i'm done", "stop", "that's all" };
            bool wantsToExit = exitKeywords.Any(kw => input.Contains(kw));

            if (wantsToExit && !string.IsNullOrEmpty(currentTopic))
            {
                bot.Append($"No problem, {Capitalise(memory["name"])}! If you need anything else, feel free to ask. Stay safe online! \n");
                currentTopic = "";
                awaitFollowUp = false;
                return bot.ToString();
            }

            //---------------------------------------------------------------------
            //loops through the each entry in the keywordResponses dictionary and checks if keywords are in the user input 
            foreach (var entry in keywordResponses)
            {
                string mainKeyword = keywordToMainTopic.ContainsKey(entry.Key) ? keywordToMainTopic[entry.Key] : entry.Key;

                //if the user input contains a keyword, then a random response from the list in the keywordResponses dictionary will be added to the string builder
                if (input.Contains(entry.Key))
                {
                    currentTopic = mainKeyword;
                    var response = entry.Value[rand.Next(entry.Value.Count)];

                    if (memory.ContainsKey("favouriteTopic") && input.Contains(memory["favouriteTopic"]))
                    {
                        bot.Append($"As someone interested in {Capitalise(memory["favouriteTopic"])}, here's a tip:\n");
                    }

                    bot.Append(response + "\n");
                    string followUp = followUpPrompts[rand.Next(followUpPrompts.Count)];
                    string userName = memory.ContainsKey("name") ? Capitalise(memory["name"]) : "friend";

                    bot.Append(followUp.Replace("{name}", userName) + "\n");
                    awaitFollowUp = true;

                    return bot.ToString();
                }
            }

            //Bot response will display if no keyword, task, quiz or activity log prompts are given by the user
            return "I'm not sure how to respond to that. Could you please rephrase your question?";
            //---------------------------------------------------------------------             
        }
        
        //----------------------------------------------------------------------------------------
        //Formats quiz question into a string with its multiple choice answers
        private string FormatQuestion(QuizQuestion q)
        {
            if (q == null) return "No more questions.";
            var sb = new StringBuilder();
            sb.AppendLine(q.Question);
            for (int i = 0; i < q.Options.Count; i++)
                sb.AppendLine($"{i + 1}. {q.Options[i]}");
            return sb.ToString();
        }
        //-------------------------------------------------------------------------------------------
        //Information on follow-up topics is stored in the followUpsAnswers dictionary
        //Maps current topic to the key in followUpsAnswers. If the key matches the current topic, the user can ask follow up questions with keywords from the subdictionary
        public void FollowUpAnswers()
        {
            followUpsAnswers["phishing"] = new Dictionary<string, string>
            {
                { "identify", "You can identify a phishing scam by looking for urgent language, generic greetings, suspicious links, and requests for sensitive info.\n\nWould you like to hear more about phishing?" },

                { "report", "If you think you've received a phishing email, report it to your email provider or your local cybersecurity authority.\n Is there anything else you would like to know about phishing?" },

                { "avoid", "You can avoid phishing by not clicking unknown links, checking the sender's email address, and using spam filters." },
                //example generated by OpenAI ChatGPT
                { "example","Sure! Here is an example of a phishing message:\n" +
                    "Subject - Urgent: Account Verification Required\n" +
                    "Email Body:\n\n" +
                    "Dear Customer,\n\n" +
                    "We have detected suspicious activity on your account. To ensure your account" +
                    " is not suspended, please verify your information immediately\n" +
                    "\n" +
                    "Click the link below to secure your account:\n" +
                    "www.fakebank-login.com\n\n" +
                    "Thank You\n" +
                    "Security Team\n" +
                    "YourBank\n\n" +
                    "This email pretends to be from a bank, uses urgent language and provides a fake link to trick users into entering their login credentials." }
            };
            // ---------------------------------------------------------------------------------------------------------------------------
            followUpsAnswers["password"] = new Dictionary<string, string>
            {
                { "set", "Having a strong password  is important because it is harder for attackers to guess or crack them." +
                  " Your password should include a combination of: \n" + "- Uppercase and lowercase letters\n" + "- Numbers\n"+"- Special characters (e.g. @, $, %, &)\n" +"- Atleast a total 12 characters\n"+"\nAvoid using common words, predictable patterns like \"1234\" or personal details like your name or birtday." },

                { "change", "It’s a good idea to change your passwords regularly, especially after a data breach or suspicious activity." },
                { "example","A good example for a password would be: Tr3e$nTheW1nd2025\n\n" +
                  "It is a long password that includes a mix of uppercase and lowercase letters, special characters, numbers, and no common words or phrases."},

                { "create","Here are some tips to help you create a strong, secure password\n:" +
                 "1. Make it long: Aim for at least 12 characters (the longer, the better).\n" +
                 "2. Use a mix of characters:Include uppercase and lowercase letters, numbers, and special characters (like `!`, `@`, `*`, etc.).\n" +
                 "3. Avoid common words: Don’t use easily guessed info like your name, birthdate, or “123456”.\n" +
                 "4. Use passphrases:Try combining random words or a sentence you can remember like: `CatFever@River2025&Sky'\n" +
                 "5. Don’t reuse passwords:Each of your accounts should have a unique password.\n" +
                 "6. Use a password manager:It helps you generate and store strong passwords safely.\n"}
            };
            // ---------------------------------------------------------------------------------------------------------------------------
            followUpsAnswers["scam"] = new Dictionary<string, string>
            {
                { "identify", "These are ways you can identify if it is a scam:\n" +
                    "1. Too Good to Be True\n" +
                    "\t- Example: \"You’ve won R1 million — just pay a small fee to claim it!\"\n" +
                    "\t- If it sounds too good to be true, it usually is.\n\n" +
                    "2. Pressure or Urgency\n" +
                    "\t- Example: \"Act now or your account will be suspended!\"\n" +
                    "\t- Scammers create panic to stop you from thinking clearly.\n\n" +
                    "3. Unusual Payment Requests\n" +
                    "\t- Example: \"Send money via gift cards, cryptocurrency, or wire transfer.\"\n" +
                    "\t- Legitimate businesses don’t ask for payments this way.\n\n" +
                    "Others way you can identify scams are:\n" +
                    "\t- When you recieve unusual payment requests\n" +
                    "\t- When a company asked you for personal details over email or text\n" +
                    "\t- When their is spelling or grammar mistakes in the message sent to you" },

                { "avoid", "It's great you want to know more! Here are ways to avoid being scammed:\n\n" +
                    "\t-Avoid clicking on suspicious links\n" +
                    "\t-Avoid sending money to people or offers you didnt expect\n" +
                    "\t-Avoid dowloading unknown attachments from emails and websites\n" +
                    "\t-Avoid trusting people just because they seem official." },

                { "report","If you have lost money or personal information, report it to the police and get a case number." +
                    " Call or visit your bank immediately and ask them to freeze all your accounts. Also report the scam to Google Safe Browsing" +
                    "or Microsoft."},
                 //example generated by OpenAI ChatGPT
                { "example","Here is an example of a scam:\n\n" +
                    "\"You’ve Won a Prize!\" WhatsApp or SMS Scam\n\n" +
                    "Congratulations! You have won R10,000 in the Pick n Pay Customer Giveaway. To claim your prize, click this link: " +
                    "[bit.ly/winnerSA] and fill in your details.\n\n" +
                    "This scam pretends to be from a real company, like Pick n Pay or Woolworths. But it’s not from the company " +
                    "at all. It uses the company’s name and logo to look trustworthy."}
            };
            // ---------------------------------------------------------------------------------------------------------------------------
            followUpsAnswers["worm"] = new Dictionary<string, string>
            {
                { "identify", "You can identify a worm by noticing unusual system behavior like slow performance, high network activity, " +
                    "or unfamiliar programs running in the background." },

                { "avoid", "To avoid a worm, keep your operating system and software updated, use strong antivirus software, avoid " +
                    "opening suspicious email attachments or links, and never download files from untrusted websites." },

                { "report","If you suspect a worm infection, disconnect your device from the network to prevent it from spreading, run " +
                    "a full antivirus scan, and report it to your IT department or local cybercrime unit such as the SAPS Cybercrime Division in South Africa."},
                 //example generated by OpenAI ChatGPT
                { "example","An example of a worm is the 'ILOVEYOU' worm, which spread via email in 2000 by tricking users into opening " +
                    "an infected attachment. Once opened, it replicated itself and spread to all contacts in the user’s address book."}
            };
            // ---------------------------------------------------------------------------------------------------------------------------
            followUpsAnswers["trojan horse"] = new Dictionary<string, string>
            {
                { "identify", "You can identify a Trojan by noticing strange behavior on your device after installing something " +
                    ", like unexpected pop-ups, new programs, or system slowdowns." },

                { "avoid", "To avoid a Trojan horse, only download software from trusted sources, avoid pirated or cracked apps, " +
                    "do not open attachments or links from unknown emails, and always use up-to-date antivirus software." },

                { "report","If you suspect a Trojan, disconnect your device from the internet, run a full antivirus scan, and report " +
                    "the incident to your cybersecurity team or a cybercrime unit like SAPS Cybercrime in South Africa."},
                 //example generated by OpenAI ChatGPT
                { "example","An example of a Trojan horse is the Zeus Trojan, which looked like legitimate software but secretly stole " +
                    "banking information from users once installed."}
            };
            // ---------------------------------------------------------------------------------------------------------------------------
            followUpsAnswers["online safety"] = new Dictionary<string, string>
            {
                { "avoid", "To stay safe online, use strong passwords, keep your devices updated, avoid clicking unknown links or " +
                    "downloading files from untrusted sources, and never share personal details (like your ID number or bank info) on unsecured websites or with strangers." },
                { "report","If you encounter something unsafe online, like cyberbullying, scams, or harmful content, report it to the" +
                    " platform (e.g., Facebook, Instagram, Google), inform your guardian or teacher, and if it’s serious, contact local authorities or a cybercrime unit."},
                { "example","An example of practicing online safety is checking that a website’s URL starts with 'https://' before entering your credit card information, " +
                    "which means the site is using a secure connection."}
            };
            // ---------------------------------------------------------------------------------------------------------------------------
            followUpsAnswers["cyber attack"] = new Dictionary<string, string>
            {
                { "identify", "A cyber attack can be identified by signs like system slowdowns, unauthorized access to accounts, " +
                    "unexpected software behavior, frequent crashes, or alerts from your antivirus software. It often involves someone trying to steal, damage, or disrupt digital data or systems." },
                { "avoid", "To avoid cyber attacks, use strong and unique passwords, enable two-factor authentication, regularly update" +
                    " your software, avoid clicking on suspicious links or attachments, and use reliable antivirus and firewall protection." },
                { "report","If you suspect a cyber attack, immediately disconnect your device from the internet, inform your IT team or " +
                "supervisor, and report it to a cybercrime authority like the SAPS Cybercrime Division in South Africa or your country's official cybersecurity agency."},
                { "types","Common types of cyber attacks include phishing (deceptive emails), ransomware (holding data hostage), denial-of-service (disrupting services), malware (infecting systems), and man-in-the-middle attacks (intercepting communication)."},
                 //example generated by OpenAI ChatGPT
                { "example","An example of a cyber attack is the WannaCry ransomware attack in 2017, which spread across the globe and locked users’ files, demanding payment in Bitcoin to unlock them."}
            };
            // ---------------------------------------------------------------------------------------------------------------------------
            followUpsAnswers["cyber security"] = new Dictionary<string, string>
            {
                { "important","Cybersecurity is vital because it safeguards our digital lives. It protects our " +
                    "personal information, financial data, and critical infrastructure from theft, damage, and disruption in an increasingly" +
                    " connected world. Without strengthened cybersecurity measures, we become vulnerable to a wide range of threats that can" +
                    " compromise our privacy, finances, and even our physical safety."},

                 { "impact","Cybersecurity is vital because it safeguards our digital lives. It protects our " +
                    "personal information, financial data, and critical infrastructure from theft, damage, and disruption in an increasingly" +
                    " connected world. Without strengthened cybersecurity measures, we become vulnerable to a wide range of threats that can" +
                    " compromise our privacy, finances, and even our physical safety."}
            };
        }
        //---------------------------------------------------------------------------------------------------------------------------
        //Populate the dictionary with key-value pairs of responses and possible user inputs
        public void KeywordResponses()
        {

            //List of responses for user greeting
            List<string> greetResponse = new List<string>
            {
                "I'm doing well, thank you! How are you?",
                "I'm doing good and yourself?"
            };
            string[] greetKeywords = { "how are you doing", "how are you", "what's up", "how have you been" };
            foreach (string keyword in greetKeywords)
            {
                keywordResponses.Add(keyword, greetResponse);
            }
            //--------------------------------------------------------------------------------------------------------------------------
            followUpPrompts = new List<string>
            {
                "Is there anything else you'd like to know, {name}?",
                "Would you like to know more about this, {name}?",
                "Do you want me to explain further, {name}?",
                "Is there something specific you'd like to dive into, {name}?",
                "Let me know if you'd like more details, {name}.",
                "Would you like to continue learning about this, {name}?",
                "Should I share an example or more tips?, {name}"
            };
            //---------------------------------------------------------------------------------------------------------------------------
            //List of responses for user optimism
            List<string> goodGreetResponse = new List<string>
            {
               "That's great to hear! How can I help you?",
                "I'm glad you're doing well :)\nHow can I help?"
            };
            string[] goodGreetKeywords = { "im good", "im great", "im doing alright", "im doing okay" };
            foreach (string keyword in goodGreetKeywords)
            {
                keywordResponses.Add(keyword, goodGreetResponse);
            }
            //---------------------------------------------------------------------------------------------------------------------------
            //List of responses for thank yous
            List<string> thankYouResponse = new List<string>
            {
               "You are welcome :) "
            };
            string[] thankKeywords = { "thanks", "thank you" };
            foreach (string keyword in thankKeywords)
            {
                keywordResponses.Add(keyword, thankYouResponse);
            }
            //---------------------------------------------------------------------------------------------------------------------------
            //List of responses for chat bot information
            List<string> infoResponse = new List<string>
            {
               "You can ask me about common cyber threats, how to recognize phishing scams, create strong passwords and how to "
                            + "secure your devices. I'm here to help you stay safe online!"
            };
            string[] infoKeywords = { "what information can you", "so what can you tell me" };
            foreach (string keyword in infoKeywords)
            {
                keywordResponses.Add(keyword, infoResponse);
            }
            //---------------------------------------------------------------------------------------------------------------------------
            //List of responses for cybersecurity keywords
            List<string> cyberSecurityResponse = new List<string>
            {
               "I will gladly tell you my purpose! My purpose is to help you identify and safely navigate real-life cyber threats."
                            + " I simulate common scams, phishing attempts, and suspicious behaviour so you can learn"
                            + " to spot and avoid them before they cause harm."
            };
            string[] cyberSecurityKeywords = { "cyber security", "what is your purpose", "what are you for", "what do you do" };
            foreach (string keyword in cyberSecurityKeywords)
            {
                keywordResponses.Add(keyword, cyberSecurityResponse);
            }
            //---------------------------------------------------------------------------------------------------------------------------
            //List of responses for cyber attack keywords
            List<string> cyberAttackResponse = new List<string>
            {
                "There are many types of cyber attacks, which includes:\n"+
                    "- Malware: Malicious software like viruses, worms, and Trojan horses designed to harm systems or steal data.\n" +
                    "- Phishing: Deceptive attempts to acquire sensitive information through fraudulent emails, messages, or websites.\n" +
                    "- Denial-of-Service (DoS) and Distributed Denial-of-Service (DDoS) attacks: Overwhelming a target with traffic to make it unavailable.\n" +
                    "- Ransomware: Malware that encrypts files and demands a ransom for their release.\n" +
                    "- Man-in-the-Middle (MitM) attacks: Intercepting communication between two parties.\n" +
                    "- SQL Injection: Exploiting vulnerabilities in databases to access or manipulate data.\n" +
                    "- Zero-day exploits: Attacks that target previously unknown software vulnerabilities.\n",
                "There are several types of cyber attacks, like phishing, malware, ransomware, denial-of-service (DoS), and man-in-the-middle attacks. Each one works differently to steal data or disrupt systems.",
                    "Common cyber attacks include phishing, malware, ransomware, and DoS attacks." +
                    "Cyber attacks come in many forms. Here are a few common ones:" +
                    "\nPhishing: Fake emails that trick you into giving away personal info." +
                    "\nMalware: Malicious software like viruses and trojans." +
                    "\nRansomware: Locks your files and demands payment." +
                    "\nDoS/DDoS: Overloads systems to make them crash." +
                    "\nMITM (Man-in-the-Middle): Intercepts data between two parties.\n"
            };
            string[] cyberKeywords = { "cyber attack", "online attack", "internet attack", "cyber threat" };
            foreach (string keyword in cyberKeywords)
            {
                keywordResponses.Add(keyword, cyberAttackResponse);
            }
            //---------------------------------------------------------------------------------------------------------------------------
            //List of responses for worm keyword
            List<string> wormResponse = new List<string>
            {
                "A worm is a kind of malware that copies itself and spreads without needing you to do anything (like clicking). " +
                "It moves from one computer or device to another over the internet, Wi-Fi, or USB. It can:\n" +
                "\t- Slow down your system" +
                "\t- Steal personal info" +
                "\t- Install more malware" +
                "\t- Even take control of your device",
                "A worm is a self-replicating type of malware that can spread across networks without needing to attach itself to a host program. Unlike viruses, " +
                "worms can multiply independently, often exploiting network vulnerabilities to infect multiple devices.\n\nAre you interested in hearing more about worms?"
            };

            keywordResponses.Add("worm", wormResponse);

            //---------------------------------------------------------------------------------------------------------------------------            
            //List of responses for trojan keyword
            List<string> trojanResponse = new List<string>
            {
                "A Trojan horse is a type of malware that disguises itself as a legitimate program or file to trick users into installing it. Once installed, " +
                    "it can carry out malicious activities, such as stealing data, installing other malware, or providing remote access to attackers.",
                "A Trojan is a type of malware (malicious software) that pretends to be something harmless or useful to trick you into downloading or opening it. " +
                    "Once it's on your device, it can steal data, spy on you, control your system, or let in other malware, all without you knowing."
            };

            keywordResponses.Add("trojan", trojanResponse);

            //---------------------------------------------------------------------------------------------------------------------------
            //List of responses for phishing keywords
            List<string> phishingResponse = new List<string>
            {
                "Phishing is a type of cyberattack where attackers trick people into revealing their personal information, for example, financial or login information. " +
                    "To avoid phishing, double-check email addresses and URLs. You can hover over links to preview where they lead.",
                "Phishing is often done through fake emails, text messages, websites or calls that appear legitamate " +
                    "Look out for online requests for personal information like passwords or PINS, legitimate companies will never ask you like this!\n",
                "Phishing occurs when attackers impersonate trusted entities like banks, organisations or companies. "+
                    "A tip to avoid phishing  is to never share your personal information over emails, SMS or social media platforms.",
                "That is a good question! Phishing is a type of social engineering attack where criminals attempt to trick you into revealing sensitive information, such as passwords," +
                            " credit card numbers, or personal details. This is often done through deceptive emails, text messages, or fake websites that mimic legitimate entities.\n"
            };
            string[] phishKeywords = { "attachment", "phish" };
            foreach (string keyword in phishKeywords)
            {
                keywordResponses.Add(keyword, phishingResponse);
            }

            //---------------------------------------------------------------------------------------------------------------------------
            //List of responses for password keywords
            List<string> passwordResponse = new List<string>
            {
                "It is important that you use strong and unique passwords for every online account. " +
                    "This means your passwords should be a mix of uppsercase and lowercase letters, numbers, and special characters. Avoid using commom words , simple sequences like \"1234\" or personal details like birthdays.",
                "It is important that you never reuse passwords across different sites. If one site gets hacked and your password is leaked" +
                    ", all other accounts with the same password will be comprimised.",
                "Having a strong password  is important because it is harder for attackers to guess or crack them." +
                    " Your password should include a combination of: \n" +
                    "- Uppercase and lowercase letters\n" +
                    "- Numbers\n"+
                    "- Special characters (e.g. @, $, %, &)\n" +
                    "- Atleast a total 12 characters\n"+
                    "\nAvoid using common words, predictable patterns like \"1234\" or personal details like your name or birtday."
            };
            string[] passwordKeywords = { "password", "credentials", "login" };
            foreach (string keyword in passwordKeywords)
            {
                keywordResponses.Add(keyword, passwordResponse);
            }

            //---------------------------------------------------------------------------------------------------------------------------
            //List of responses for online safety keywords
            List<string> onlineSafetyResponse = new List<string>
            {
                "Online safety means protecting yourself and your online information when you're using the internet. " +
                    "It involves being careful about what you share, who you talk to and the websites or apps you use.",
                "Online safety is all about using the internet in a smart and secure way. " +
                    "Creating strong passwords, withholding personal information from strangers and avoiding suspicious links are examples of good habits to keep you safe online.",
                "Online safety is basically about staying safe while you're online, whether you're chatting, browsing or playing games. "+
                    "It's about knowing what to click, who to trust and how to protect your info from the wrong people.",
                "Staying safe online involves several key practices:\n\n" +
                            "- Use strong, unique passwords and a password manager.\n" +
                            "- Enable two-factor authentication.\n" +
                            "- Be cautious of suspicious emails, links, and attachments.\n" +
                            "- Keep your software updated.\n" +
                            "- Use reputable antivirus and anti-malware software.\n" +
                            "- Be mindful of what you share online.\n" +
                            "- Use secure Wi-Fi networks and consider a VPN on public networks.\n" +
                            "- Be wary of pop-ups and unexpected downloads.\n" +
                            "- Stay informed about online threats and security best practices.\n"
            };
            string[] onlineSafetyKeywords = { "online safety", "safe online", "online security", "protection", "privacy protection" };
            foreach (string keyword in onlineSafetyKeywords)
            {
                keywordResponses.Add(keyword, onlineSafetyResponse);
            }
            //---------------------------------------------------------------------------------------------------------------------------
            //List of responses for scam keywords
            List<string> scamResponse = new List<string>
            {
                "A scam is a dishonest scheme designed to trick people into giving away their money, personal information or access secure systems." +
                    "Scammers often pretend to be trustworthy individuals or organisations, like banks or government departments, to gain your trust\n",
                "Scammers use psychological manipulation, technical tricks, or social engineering to exploit trust, fear, urgency, or greed. " +
                "A scam is a trick that someone uses to cheat people out of money or personal information. The scammer pretends to be someone " +
                    "trustworthy, but their goal is to steal or deceive.",
                "An online scam is a cybercrime where attackers use fake websites, emails, or social media profiles to trick users into clicking " +
                    "malicious links, giving away login credentials, or sending payments to fake accounts."
            };
            string[] scamKeywords = { "fraud", "scam", "hoax", "sham" };
            foreach (string keyword in scamKeywords)
            {
                keywordResponses.Add(keyword, scamResponse);
            }
            //---------------------------------------------------------------------------------------------------------------------------
        }
        //-------------------------------------------------------------------------------------------------------------------------------
        //Maps synonyms to followUpsAnswers keys so that the current topic is associated with the key retrieves the given values in the dictionary.
        public void KeywordToMainTopic()
        {
            keywordToMainTopic = new Dictionary<string, string>
            {
                { "attachment", "phishing" },

                { "login", "password" },
                { "credential", "password" },

                { "fraud", "scam" },
                { "hoax", "scam" },
                { "sham", "scam" },

                { "safe online", "online safety" },
                { "online security", "online safety" },
                { "protection", "online safety" },
                { "privacy protection", "online safety" },

                { "online attack", "cyber attack" },
                { "internet attack", "cyber attack" },
                { "cyber threat", "cyber threat" }
            };
        }

        //--------------------------------------------------------------------------------------------------------------------------------
        //Initialize negative, positive and neutral keyword arrays for sentiment detection and responses
        public void InitialiseSentimentResponses()
        {
            string[] negativeKeywords = { "anxious", "concerned", "troubled", "uneasy", "nervous", "distressed", "worry", "worried", "terrified", "frightened", "panic", "stressed", "frustrated", "apprehensive" };
            List<string> negativeResponses = new List<string>
            {
                "It's perfectly normal to be worried about your online safety! And it's very good that you're educating yourself on the matter :)",
                "Many people feel anxious about online safety. I'm here to help you through it",
                "Don't worry, I will help you understand how to stay safe online."
            };

            foreach (var k in negativeKeywords)
            {
                sentimentResponses[k] = negativeResponses;
            }

            string[] positiveKeywords = { "excited", "happy", "relieved", "confident", "motivated" };
            List<string> positiveResponses = new List<string>
            {
                "That's awesome to hear! Let's keep learning.",
                $"I love the energy {(memory.ContainsKey("name") ? memory["name"] : "friend")}! What else would you like to know?"
            };

            foreach (var k in positiveKeywords)
            {
                sentimentResponses[k] = positiveResponses;
            }

            string[] neutralKeywords = { "curious", "interested", "wondering", "thinking", "inquiring" };
            List<string> neutralResponses = new List<string>
            {
                $"That's a great question {(memory.ContainsKey("name") ? memory["name"] : "friend")}. Let me help you with that!",
                "Curiosity is the first step to learnng. Here's what I know:"
            };

            foreach (var k in neutralKeywords)
            {
                sentimentResponses[k] = neutralResponses;
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        //Capitalises user data from the memory dictionary
        private string Capitalise(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return s;
            return char.ToUpper(s[0]) + s.Substring(1);
        }
    }
}

//------------------------------------------------------------END OF FILE--------------------------------------------------------