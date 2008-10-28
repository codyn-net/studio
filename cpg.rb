$:.unshift(File.join(File.dirname(__FILE__)))

def require_files(d)
	full = File.join(File.dirname(__FILE__), d)

	Dir.entries(full).each do |f|
		next unless File.file?(File.join(full, f))
		next unless f =~ /\.rb$/
		
		require "#{d}/#{f}"
	end
end

# make sure all component classes and group renderers are loaded
require_files('components')
require_files('groups')

require 'application'

app = Cpg::Application.new
app.run



