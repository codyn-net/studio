require 'components/state'
require 'simulation'
require 'cairo'
require 'mathcontext'

module Cpg::Components
	class Relay < State
		def initialize
			super

			@patouter = Cairo::LinearPattern.new(0, 1, 0, 0)
			@patouter.add_color_stop_rgb(0, 200 / 255.0, 200 / 255.0, 100 / 255.0)
			@patouter.add_color_stop_rgb(self.allocation.height, 1, 1, 1)

			@patinner = Cairo::LinearPattern.new(0, 1, 0, 0)
			@patinner.add_color_stop_rgb(0, 150 / 255.0, 150 / 255.0, 100 / 255.0)
			@patinner.add_color_stop_rgb(self.allocation.height * 0.8, 1, 1, 1)
		end
		
		def line_color
			[130 / 255.0, 130 / 255.0, 30 / 255.0]
		end
	end
end
