//Copyright 2019 Volodymyr Podshyvalov
//
//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this file except in compliance with the License.
//You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//limitations under the License.

WHAT IS AUTHORITY CONTROLLER?
AuthorityController it's a library that allow easy integration of 
an authentication system, that provides possibility to control
users' rights, data, bans, etc.

Moreover provides base security like managing of user password in 
hashed and salted state.

Furthermore provides possibilities to use this controller in servers'
architecture that contains a list of releted servers that need to
know only the tokens data but have no authority to operate private
personalized users' data.