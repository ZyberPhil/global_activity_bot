import React from 'react';
import ReactMarkdown from 'react-markdown';

const privacyMarkdown = '# Privacy Policy for [OrbitXP]\n\n' +
'\nThis Privacy Policy governs the collection, use, and sharing of personal information by [OrbitXP], a Discord bot developed by [Phil Hendrik Hempel]. By using [OrbitXP], you agree to the terms of this Privacy Policy.\n\n' +
'## Information We Collect\n\n' +
'We collect information that you provide to us through your use of the bot, such as your Discord user ID and username, server and channel information, and message content. We may also collect usage data, such as the frequency and duration of your use of the bot.\n\n' +
'## How We Use Your Information\n\n' +
'We use your information to operate and improve [OrbitXP], including to provide support and respond to your requests. We may also use your information to develop new features or services, to conduct research and analytics, and to comply with legal obligations.\n\n' +
'## Sharing Your Information\n\n' +
'We do not sell or share your personal information with third parties. However, we may disclose your information in response to legal process or a request from a law enforcement agency or regulatory authority.\n\n' +
'## Data Retention\n\n' +
'We retain your information for as long as necessary to provide [OrbitXP]’s services or as required by law. We will delete your information upon your request or when it is no longer needed.\n\n' +
'## Data Security\n\n' +
'We take reasonable measures to protect your information from unauthorized access, alteration, or destruction. However, no security measure is perfect, and we cannot guarantee the security of your information.\n\n' +
'## Changes to this Policy\n\n' +
'We may update this Privacy Policy from time to time, and we will post the updated policy on our website. Your continued use of [OrbitXP] after we make changes to this policy indicates your acceptance of the revised policy.\n\n' +
'## Contact Us\n\n' +
'If you have any questions or concerns about this Privacy Policy, please contact us at [philhempel4@gmail.com].\n\n' +
'## Effective Date\n\n' +
'This Privacy Policy is effective as of [22.11.2025].\n';

const PrivacyPolicy = () => {
  return (
    <div className="content-container">
      <h1>Privacy Policy</h1>
      <ReactMarkdown>{privacyMarkdown}</ReactMarkdown>
    </div>
  );
};

export default PrivacyPolicy;
